using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using StockManagementWebApi.Common.Controllers;
using StockManagementWebApi.Models;
using StockManagementWebApi.Models.NonStockCII;
using StockManagementWebApi.Services;

namespace StockManagementWebApi.Controllers
{
	[Route("api/[controller]")]
	public class SmInboundStockNonCiisController : BaseApiController
	{
		private readonly INonCiiStockService _nonCiiStockService;
		private readonly IWebHostEnvironment _environment;
		private readonly MydbContext _context;

		public SmInboundStockNonCiisController(INonCiiStockService nonCiiStockService, IWebHostEnvironment environment, MydbContext context)
		{
			_nonCiiStockService = nonCiiStockService;
			_environment = environment;
			_context = context;
		}

		// ------------------------------------------------------------------ Listings

		// GET: api/SmInboundStockNonCiis/GetSmInboundNonStockCiis/{UserName}
		[HttpGet("GetSmInboundNonStockCiis/{UserName}")]
		public async Task<IActionResult> GetSmInboundNonStockCiis(string UserName, CancellationToken cancellationToken)
		{
			var stock = await _nonCiiStockService.GetStockListAsync(UserName, cancellationToken);

			return Success(stock, "Non-CII stock list retrieved successfully.");
		}

		[HttpGet("GetInwardNonStockCiis/{MaterialNumber}/{name}")]
		public async Task<IActionResult> GetInwardNonStockCiis(string MaterialNumber, string name, CancellationToken cancellationToken)
		{
			var inward = await _nonCiiStockService.GetInwardListAsync(MaterialNumber, name, cancellationToken);

			return Success(inward, "Non-CII inward list retrieved successfully.");
		}

		// ------------------------------------------------------------------ Bulk operations

		[HttpPost("bulk-update")]
		public async Task<IActionResult> BulkUpdate([FromBody] BulkUpdateRequest request, CancellationToken cancellationToken)
		{
			await _nonCiiStockService.BulkUpdateSerialsAsync(request, cancellationToken);

			return Updated(message: "Updated successfully");
		}

		[HttpPost("BulkImportNonCII")]
		public async Task<IActionResult> BulkImportNonCII([FromForm] AddNonCIIStockInward data)
		{
			if (data.file == null || data.file.Length == 0)
				return BadRequest("No file uploaded.");

			var uploadsDirectory = Path.Combine(_environment.ContentRootPath, "Uploads");
			if (!Directory.Exists(uploadsDirectory))
				Directory.CreateDirectory(uploadsDirectory);

			var filePath = Path.Combine(uploadsDirectory, Guid.NewGuid() + Path.GetExtension(data.file.FileName));

			var rowResults = new List<StockImportRowResult>();

			try
			{
				using (var stream = new FileStream(filePath, FileMode.Create))
				{
					await data.file.CopyToAsync(stream);
				}

				ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
				using var package = new ExcelPackage(new FileInfo(filePath));
				var worksheet = package.Workbook.Worksheets[0];
				if (worksheet.Dimension == null)
					return BadRequest("Excel file is empty.");

				int rowCount = worksheet.Dimension.Rows;

				// Get tenant code
				var tenentcode = await _context.Database
					.SqlQuery<string>($"SELECT Fk_TenentCode AS Value FROM [dbo].[sm_Users] WHERE LoginId = {data.UserName}")
					.FirstOrDefaultAsync();

				if (string.IsNullOrEmpty(tenentcode))
					return BadRequest($"No tenant found for user '{data.UserName}'.");

				for (int row = 2; row <= rowCount; row++)
				{
					var materialNumber = worksheet.Cells[row, 1].Text.Trim();
					if (string.IsNullOrWhiteSpace(materialNumber))
						continue;

					var materialDescription = worksheet.Cells[row, 2].Text.Trim();

					try
					{
						// Check if material number exists for this tenant
						var isMaterialNumberAvailable = await _context.Database
							.SqlQuery<int>($@"
                        SELECT 1 AS Value
                        FROM [dbo].[sm_material_master] smm
                        INNER JOIN [dbo].[sm_Users] su ON su.Pk_UserCode = smm.Fk_UserCode
                        WHERE smm.MaterialNumber = {materialNumber}
                          AND su.Fk_TenentCode = {tenentcode}")
							.AnyAsync();

						if (!isMaterialNumberAvailable)
						{
							await _context.Database.ExecuteSqlRawAsync(
								@"EXEC AddNonStockCII_MaterialNumber @p0, @p1, @p2",
								data.UserName, materialNumber, materialDescription);
						}

						int quantity = 0;
						int.TryParse(worksheet.Cells[row, 3].Text, out quantity);
						var status = worksheet.Cells[row, 4].Text.Trim();

						await _context.Database.ExecuteSqlRawAsync(
							@"exec Sp_AddInboundStock_NonCII @p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11, @p12",
							data.DeliveryNumber, data.OrderNumber, materialNumber,
							materialDescription, data.InwardDate, data.InwardFrom, data.ReceivedBy,
							data.RackLocation, quantity, data.UserName, data.PoNumber, data.Location, status);

						rowResults.Add(new StockImportRowResult
						{
							RowNumber = row,
							MaterialNumber = materialNumber,
							SerialNumber = "", // Non-CII stock has no serial number per row
							Success = true,
							Message = "Imported successfully."
						});
					}
					catch (SqlException sqlEx)
					{
						rowResults.Add(new StockImportRowResult
						{
							RowNumber = row,
							MaterialNumber = materialNumber,
							SerialNumber = "",
							Success = false,
							Message = GetFriendlySqlMessage(sqlEx)
						});
					}
					catch (Exception rowEx)
					{
						rowResults.Add(new StockImportRowResult
						{
							RowNumber = row,
							MaterialNumber = materialNumber,
							SerialNumber = "",
							Success = false,
							Message = rowEx.Message
						});
					}
				}

				if (rowResults.Count == 0)
					return BadRequest("The Excel file contains no data.");

				var successCount = rowResults.Count(r => r.Success);
				var failCount = rowResults.Count(r => !r.Success);

				return Ok(new
				{
					TotalRows = rowResults.Count,
					SuccessCount = successCount,
					FailCount = failCount,
					Results = rowResults
				});
			}
			catch (Exception ex)
			{
				return StatusCode(500, new
				{
					Success = false,
					Message = ex.Message
				});
			}
			finally
			{
				if (System.IO.File.Exists(filePath))
					System.IO.File.Delete(filePath);
			}
		}

		// Reuse GetFriendlySqlMessage / StockImportRowResult if already declared elsewhere in this controller class.
		// Translates raw SQL errors into user-friendly messages.
		// SQL error 2627 = PK/unique constraint violation, 2601 = duplicate key on unique index.
		private static string GetFriendlySqlMessage(SqlException sqlEx)
		{
			if (sqlEx.Number == 2627 || sqlEx.Number == 2601)
			{
				if (sqlEx.Message.Contains("MaterialNumber", StringComparison.OrdinalIgnoreCase)
					|| sqlEx.Message.Contains("Material", StringComparison.OrdinalIgnoreCase))
				{
					return "Material Number already exists.";
				}
				return "Duplicate entry — this record already exists.";
			}
			if (sqlEx.Number == 547) // foreign key violation
				return "This record references data that doesn't exist (invalid reference).";
			if (sqlEx.Number == 8152) // string/binary data truncation
				return "One of the values is too long for its field.";
			return "Import failed for this row due to a database error.";
		}


		[HttpPost("NonStockCIIMaterial")]
		public async Task<IActionResult> AddMaterialNumber([FromBody] AddMaterial data, CancellationToken cancellationToken)
		{
			await _nonCiiStockService.AddMaterialAsync(data, cancellationToken);

			return Success(message: "Material number added successfully.");
		}

		// ------------------------------------------------------------------ Inbound

		[HttpPost("AddNonStockInbounddata")]
		public async Task<IActionResult> AddNonStockInbounddata([FromBody] AddNonStockInward data, CancellationToken cancellationToken)
		{
			await _nonCiiStockService.AddInboundAsync(data, cancellationToken);

			return Success(message: "Inbound data added successfully.");
		}

		[HttpPost("DeleteNonStockInbounddata/{MaterialNumber}/{DeliveryNumber}/{InboundStockNonCIIKey}/{name}")]
		public async Task<IActionResult> DeleteNonStockInbounddata(string MaterialNumber, string DeliveryNumber, string InboundStockNonCIIKey, string name, CancellationToken cancellationToken)
		{
			await _nonCiiStockService.DeleteInboundAsync(MaterialNumber, DeliveryNumber, InboundStockNonCIIKey, name, cancellationToken);

			return Deleted(message: "Inbound data deleted successfully.");
		}

		[HttpPost("UpdateNonStockInbounddata")]
		public async Task<IActionResult> UpdateNonStockInbounddata([FromBody] UpdateNonStockInward data, CancellationToken cancellationToken)
		{
			await _nonCiiStockService.UpdateInboundAsync(data, cancellationToken);

			return Updated(message: "Inbound data updated successfully.");
		}

		// ------------------------------------------------------------------ Outbound

		[HttpPost("AddNonStockOutbounddata")]
		public async Task<IActionResult> AddNonStockOutbounddata([FromBody] AddOutBoundNonStockCII data, CancellationToken cancellationToken)
		{
			await _nonCiiStockService.AddOutboundAsync(data, cancellationToken);

			return Success(message: "Outbound data added successfully.");
		}

		[HttpPost("BulkAddNonStockOutbound")]
		public async Task<IActionResult> BulkAddNonStockOutbound([FromBody] List<BulkAddOutboundDataNonStockCii> dataList, CancellationToken cancellationToken)
		{
			await _nonCiiStockService.BulkAddOutboundAsync(dataList, cancellationToken);

			return Success(message: "Bulk upload completed successfully.");
		}

		[HttpGet("DeliveredDataList/{MaterialNumber}/{UserName}")]
		public async Task<IActionResult> GetDeliveredDataList(string MaterialNumber, string UserName , CancellationToken cancellationToken)
		{
			var delivered = await _nonCiiStockService.GetDeliveredListAsync(MaterialNumber, UserName , cancellationToken);

			return Success(delivered, "Delivered data retrieved successfully.");
		}

		[HttpPost("DeleteNonStockDeliverdata/{MaterialNumber}/{DeliveryNumber}/{OutboundStockNonCIIKey}/{UserName}")]
		public async Task<IActionResult> DeleteNonStockDeliverdata(string MaterialNumber, string DeliveryNumber, string OutboundStockNonCIIKey, string UserName, CancellationToken cancellationToken)
		{
			await _nonCiiStockService.DeleteDeliveredAsync(MaterialNumber, DeliveryNumber, OutboundStockNonCIIKey, UserName, cancellationToken);

			return Deleted(message: "Non-stock delivery data deleted and inbound stock updated successfully.");
		}

		[HttpPost("UpdateNonStockDeliverdata")]
		public async Task<IActionResult> UpdateNonStockDeliverdata([FromBody] UpdateNonStockDeliverData data, CancellationToken cancellationToken)
		{
			await _nonCiiStockService.UpdateDeliveredAsync(data, cancellationToken);

			return Updated(message: "Inbound and outbound stock updated successfully.");
		}

		// ------------------------------------------------------------------ Returns

		[HttpPost("AddNonStockReturndata")]
		public async Task<IActionResult> AddNonStockReturndata([FromBody] AddNonStockReturnData data, CancellationToken cancellationToken)
		{
			await _nonCiiStockService.AddReturnAsync(data, cancellationToken);

			return Success(message: "Return data added successfully.");
		}

		[HttpPost("UpdateNonStockReturndata")]
		public async Task<IActionResult> UpdateNonStockReturndata([FromBody] UpdateNonStockRetundata data, CancellationToken cancellationToken)
		{
			await _nonCiiStockService.UpdateReturnAsync(data, cancellationToken);

			return Updated(message: "Return data updated successfully.");
		}

		[HttpPost("UpdateNonStockReturnType/{MaterialNumber}/{OrderNumber}")]
		public async Task<IActionResult> UpdateNonStockReturnType(string MaterialNumber, string OrderNumber, CancellationToken cancellationToken)
		{
			await _nonCiiStockService.UpdateReturnTypeAsync(MaterialNumber, OrderNumber, cancellationToken);

			return Updated(message: "Return type updated successfully.");
		}

		[HttpPost("DeleteNonStockReturnData/{MaterialNumber}/{OrderNumber}")]
		public async Task<IActionResult> DeleteNonStockReturnData(string MaterialNumber, string OrderNumber, CancellationToken cancellationToken)
		{
			await _nonCiiStockService.DeleteReturnAsync(MaterialNumber, OrderNumber, cancellationToken);

			return Deleted(message: "Return data deleted successfully.");
		}

		[HttpPost("GetNonStockReturnData/{MaterialNumber}")]
		public async Task<IActionResult> GetNonStockReturnData(string MaterialNumber, CancellationToken cancellationToken)
		{
			var returns = await _nonCiiStockService.GetReturnListAsync(MaterialNumber, cancellationToken);

			return Success(returns, "Return data retrieved successfully.");
		}

		// ------------------------------------------------------------------ Used stock

		[HttpPost("GetNonStockUsedData/{MaterialNumber}")]
		public async Task<IActionResult> GetNonStockUsedData(string MaterialNumber, CancellationToken cancellationToken)
		{
			var usedStock = await _nonCiiStockService.GetUsedStockListAsync(MaterialNumber, cancellationToken);

			return Success(usedStock, "Used stock retrieved successfully.");
		}

		[HttpPost("AddNonStockUsedData")]
		public async Task<IActionResult> AddNonStockUsedData([FromBody] AddUsedStock data, CancellationToken cancellationToken)
		{
			await _nonCiiStockService.AddUsedStockAsync(data, cancellationToken);

			return Success(message: "Used stock added successfully.");
		}

		[HttpPost("UpdateNonStockUsedData")]
		public async Task<IActionResult> UpdateNonStockUsedData([FromBody] UpdateUsedStock data, CancellationToken cancellationToken)
		{
			await _nonCiiStockService.UpdateUsedStockAsync(data, cancellationToken);

			return Updated(message: "Used stock updated successfully.");
		}

		[HttpPost("DeleteNonStockUsedData/{MaterialNumber}/{OrderNumber}")]
		public async Task<IActionResult> DeleteNonStockUsedData(string MaterialNumber, string OrderNumber, CancellationToken cancellationToken)
		{
			await _nonCiiStockService.DeleteUsedStockAsync(MaterialNumber, OrderNumber, cancellationToken);

			return Deleted(message: "Used stock deleted successfully.");
		}

		// ------------------------------------------------------------------ Dashboards

		[HttpPost("DashBoard/{UserName}")]
		public async Task<IActionResult> DashBoard(string UserName, CancellationToken cancellationToken)
		{
			var dashboard = await _nonCiiStockService.GetDashboardAsync(UserName, cancellationToken);

			return Success(dashboard, "Dashboard retrieved successfully.");
		}

		[HttpPost("AnalyticsCII/{MaterialNumber}/{UserName}")]
		public async Task<IActionResult> AnalyticsCII(string MaterialNumber, string UserName, CancellationToken cancellationToken)
		{
			var analytics = await _nonCiiStockService.GetCiiAnalyticsAsync(MaterialNumber, UserName, cancellationToken);

			return Success(analytics, "CII analytics retrieved successfully.");
		}

		[HttpPost("AnalyticsNonCII/{MaterialNumber}/{UserName}")]
		public async Task<IActionResult> AnalyticsNonCII(string MaterialNumber, string UserName , CancellationToken cancellationToken)
		{
			var analytics = await _nonCiiStockService.GetNonCiiAnalyticsAsync(MaterialNumber, UserName , cancellationToken);

			return Success(analytics, "Non-CII analytics retrieved successfully.");
		}

		[HttpPost("DashBoardCharts/{Username}")]
		public async Task<IActionResult> DashBoardCharts(string Username, CancellationToken cancellationToken)
		{
			var charts = await _nonCiiStockService.GetDashboardChartsAsync(Username, cancellationToken);

			return Success(charts, "Dashboard charts retrieved successfully.");
		}

		// ------------------------------------------------------------------ Entity CRUD

		// GET: api/SmInboundStockNonCiis/5
		[HttpGet("{id}")]
		public async Task<IActionResult> GetSmInboundStockNonCii(string id, CancellationToken cancellationToken)
		{
			var stock = await _nonCiiStockService.GetByIdAsync(id, cancellationToken);

			return Success(stock, "Inbound non-CII stock retrieved successfully.");
		}

		// PUT: api/SmInboundStockNonCiis/5
		[HttpPut("{id}")]
		public async Task<IActionResult> PutSmInboundStockNonCii(string id, SmInboundStockNonCii smInboundStockNonCii, CancellationToken cancellationToken)
		{
			await _nonCiiStockService.UpdateAsync(id, smInboundStockNonCii, cancellationToken);

			return Updated(message: "Inbound non-CII stock updated successfully.");
		}

		// POST: api/SmInboundStockNonCiis
		[HttpPost]
		public async Task<IActionResult> PostSmInboundStockNonCii(SmInboundStockNonCii smInboundStockNonCii, CancellationToken cancellationToken)
		{
			var created = await _nonCiiStockService.CreateAsync(smInboundStockNonCii, cancellationToken);

			return Created(created, "Inbound non-CII stock created successfully.", Url.Action(nameof(GetSmInboundStockNonCii), new { id = created.DeliveryNumber }));
		}

		// DELETE: api/SmInboundStockNonCiis/5
		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteSmInboundStockNonCii(string id, CancellationToken cancellationToken)
		{
			await _nonCiiStockService.DeleteAsync(id, cancellationToken);

			return Deleted(message: "Inbound non-CII stock deleted successfully.");
		}
	}
}
