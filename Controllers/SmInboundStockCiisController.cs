using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using OfficeOpenXml;
using StockManagementWebApi.Models;
using StockManagementWebApi.Models.NonStockCII;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace StockManagementWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SmInboundStockCiisController : ControllerBase
    {
        private readonly MydbContext _context;
		private readonly IWebHostEnvironment _environment;
		private readonly IConfiguration _configuration;

		public SmInboundStockCiisController(IWebHostEnvironment environment, IConfiguration configuration,MydbContext context)
        {
			_environment = environment;
			_configuration = configuration;
			_context = context;
        }

        // GET: api/SmInboundStockCiis
        [HttpGet("GetSmInboundStockCiis/{UserName}")]
        public async Task<ActionResult> GetSmInboundStockCiis(string UserName)
        {
			var customers = _context.StockCiiLists.FromSqlRaw(@"exec StockCIIList @p0", UserName).ToList();
			return Ok(customers);
			//return await _context.SmInboundStockCiis.ToListAsync();
        }
		[HttpGet("GetReportStockCiis/{UserName}")]
		public async Task<ActionResult> GetReportStockCiis(string UserName)
		{
			var customers = _context.ReportCiis.FromSqlRaw(@"exec ReportCIIList @p0", UserName).ToList();
			return Ok(customers);
			//return await _context.SmInboundStockCiis.ToListAsync();
		}

        [HttpGet("GetLogmanagementRecord")]
        public async Task<ActionResult> GetLogmanagementRecord()
        {
            var logRecords = await _context.Log_records.FromSqlRaw("SELECT * FROM Log_record").ToListAsync();
            return Ok(logRecords);
            //return await _context.SmInboundStockCiis.ToListAsync();
        }
		[HttpPost("compare")]
		public async Task<IActionResult> CompareMaterials([FromForm] ExcelCompareRequest data)
		{
			if (data.file == null || data.file.Length == 0)
				return BadRequest("No file uploaded.");

			var uploadsDirectory = Path.Combine(_environment.ContentRootPath, "Uploads");
			if (!Directory.Exists(uploadsDirectory))
				Directory.CreateDirectory(uploadsDirectory);

			var filePath = Path.Combine(uploadsDirectory, data.file.FileName);
			await using (var stream = new FileStream(filePath, FileMode.Create))
			{
				await data.file.CopyToAsync(stream);
			}

			var excelData = new List<(string PoolName, string MaterialNumber, int ExcelStatus)>();
			var resultList = new List<MaterialComparisonResult>();

			try
			{
				ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
				using var package = new ExcelPackage(new FileInfo(filePath));
				var worksheet = package.Workbook.Worksheets[0];
				int rowCount = worksheet.Dimension.Rows;

				for (int row = 2; row <= rowCount; row++)
				{
					string poolName = worksheet.Cells[row, 2].Text?.Trim();
					string materialNumber = worksheet.Cells[row, 3].Text?.Trim();
					int.TryParse(worksheet.Cells[row, 12].Text?.Trim(), out int excelStatus);

					if (!string.IsNullOrEmpty(materialNumber))
					{
						excelData.Add((poolName, materialNumber, excelStatus));
					}
				}

				// 1. Build distinct, comma-separated material list for SQL
				var distinctMaterialNumbers = excelData.Select(x => x.MaterialNumber).Distinct();
				string materialParam = string.Join(",", distinctMaterialNumbers.Select(m => $"'{m}'"));

				// 2. Query DB once
				var dbData = await _context.StockCiiLists
					.FromSqlRaw("EXEC MaterialCIIListBulk @p0, @p1", data.UserName, materialParam)
					.ToListAsync();

				// 3. Build a lookup dictionary
				var dbLookup = dbData.ToDictionary(x => x.materialNumber, StringComparer.OrdinalIgnoreCase);

				// 4. Match Excel to DB
				foreach (var row in excelData)
				{
					dbLookup.TryGetValue(row.MaterialNumber, out var dbItem);

					resultList.Add(new MaterialComparisonResult
					{
						PoolName = row.PoolName,
						ExcelMaterialNumber = row.MaterialNumber,
						ExcelStatus = row.ExcelStatus,
						DbMaterialNumber = dbItem?.materialNumber,
						newstock = dbItem?.newstock,
						usedstock = dbItem?.usedstock,
						Damaged = dbItem?.Damaged,
						BreakFix = dbItem?.BreakFix
					});
				}

				return Ok(resultList);
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"Error: {ex.Message}");
			}
			finally
			{
				if (System.IO.File.Exists(filePath))
					System.IO.File.Delete(filePath);
			}
		}




		[HttpPost("AddBulkMaterialStock")]
		public async Task<IActionResult> Importstockdate1(AddStockInward data)
		{
			if(data.file == null || data.file.Length == 0)
				return  (BadRequest("No file uploaded."));
            var userCodes = await _context.Database.SqlQueryRaw<string>("SELECT Pk_UserCode FROM sm_users WHERE loginId = @p0", data.UserName).ToListAsync();
            // Define the uploads directory
            var uploadsDirectory = Path.Combine(_environment.ContentRootPath, "Uploads");
			// Create the directory if it doesn't exist
			if (!Directory.Exists(uploadsDirectory))
			{
				Directory.CreateDirectory(uploadsDirectory);
			}
			// Full file path
			var filePath = Path.Combine(uploadsDirectory, data.file.FileName);
			try
			{
				// Save the uploaded file
				using (var stream = new FileStream(filePath, FileMode.Create))
				{
					data.file.CopyTo(stream);
				}
				// Read data from Excel
				var inboundStocks = new List<Dictionary<string, object>>();
				ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
				using (var package = new ExcelPackage(new FileInfo(filePath)))
				{
					var worksheet = package.Workbook.Worksheets[0]; // Assuming data is in the first sheet
					var rowCount = worksheet.Dimension.Rows;
					for (int row = 2; row <= rowCount; row++) // Assuming first row is the header
					{
						if (worksheet.Cells[row, 1].Text != "")
						{
							var stock = new Dictionary<string, object>
							{

								{ "MaterialNumber", worksheet.Cells[row, 1].Text },
								{ "SerialNumber", worksheet.Cells[row, 2].Text },
								{ "Quantity", int.TryParse(worksheet.Cells[row, 3].Text, out int qty) ? qty : 0 },
								{ "Status", worksheet.Cells[row, 4].Text }
							};
							inboundStocks.Add(stock);
						}
					}


				}
				if (inboundStocks.Count == 0)
					return BadRequest("The Excel file contains no data.");

				foreach (var stock in inboundStocks)
				{
					await _context.Database.ExecuteSqlRawAsync(@"exec Addsinglestock @p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10 ,@p11,@p12", data.UserName, data.DeliveryNumber, data.OrderNumber, stock["MaterialNumber"],
						data.MaterialDescription, stock["SerialNumber"],
				stock["Quantity"], data.Inwarddate, data.InwardFrom, data.ReceivedBy, stock["Status"], userCodes[0], data.RacKLocation);
				}

				return Ok("Data imported successfully.");
			}

			catch (Exception ex)
			{
				return (StatusCode(500, $"An error occurred: {ex.Message}"));
			}
			finally
			{
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);

            }

        }

        [HttpPost("import")]
		public async Task<IActionResult> ImportStockData( AddStockInward data)
		{

			if (data.file == null || data.file.Length == 0)
				return BadRequest("No file uploaded.");
			var userCodes = await _context.Database.SqlQueryRaw<string>("SELECT Pk_UserCode FROM sm_users WHERE loginId = @p0", data.UserName).ToListAsync();


			// Define the uploads directory
			var uploadsDirectory = Path.Combine(_environment.ContentRootPath, "Uploads");

			// Create the directory if it doesn't exist
			if (!Directory.Exists(uploadsDirectory))
			{
				Directory.CreateDirectory(uploadsDirectory);
			}

			// Full file path
			var filePath = Path.Combine(uploadsDirectory, data.file.FileName);

			try
			{
				// Save the uploaded file
				using (var stream = new FileStream(filePath, FileMode.Create))
				{
					await data.file.CopyToAsync(stream);
				}

				// Read data from Excel
				var inboundStocks = new List<Dictionary<string, object>>();
				ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

				using (var package = new ExcelPackage(new FileInfo(filePath)))
				{
					var worksheet = package.Workbook.Worksheets[0]; // Assuming data is in the first sheet
					var rowCount = worksheet.Dimension.Rows;

					for (int row = 2; row <= rowCount; row++) // Assuming first row is the header
					{
						if (worksheet.Cells[row, 1].Text !="")
						{
							var stock = new Dictionary<string, object>
					{

							{ "SerialNumber", worksheet.Cells[row, 1].Text },
							{ "Quantity", int.TryParse(worksheet.Cells[row, 2].Text, out int qty) ? qty : 0 },
							{ "Status", worksheet.Cells[row, 3].Text }
							//{ "DeliveryNumber", worksheet.Cells[row, 4].Text },
							//{ "DeliveryNumber", worksheet.Cells[row, 1].Text },																																																																																																																					
							//{ "OrderNumber", worksheet.Cells[row, 2].Text },
							//{ "MaterialNumber", worksheet.Cells[row, 3].Text },
							//{ "MaterialDescription", worksheet.Cells[row, 3].Text },
							//{ "SerialNumber", worksheet.Cells[row, 4].Text },
							//{ "Quantity", int.TryParse(worksheet.Cells[row, 5].Text, out int qty) ? qty : 0 },
							//{ "InwardDate", DateTime.TryParse(worksheet.Cells[row, 6].Text, out DateTime rcvDate) ? rcvDate : (DateTime?)null },
							//{ "SourceLocation", worksheet.Cells[row, 8].Text },
							//{ "ReceivedBy", worksheet.Cells[row, 9].Text },
							//{ "Status", worksheet.Cells[row, 10].Text },
							//{ "RackLocation", worksheet.Cells[row, 11].Text },
						};
							inboundStocks.Add(stock);
						}
					}
				}

				if (inboundStocks.Count == 0)
					return BadRequest("The Excel file contains no data.");

				// Insert data into the database
				//var connectionString = _configuration.GetConnectionString("MyDBConnection");
				//using (var connection = new SqlConnection(connectionString))
				//{
				//	await connection.OpenAsync();

				foreach (var stock in inboundStocks)
				{
                    await _context.Database.ExecuteSqlRawAsync(@"exec Addsinglestock @p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10 ,@p11,@p12",data.UserName, data.DeliveryNumber, data.OrderNumber, data.MaterialNumber, 
						data.MaterialDescription, stock["SerialNumber"],
				stock["Quantity"], data.Inwarddate, data.InwardFrom, data.ReceivedBy, stock["Status"], userCodes[0], data.RacKLocation);

                    //var query = @"
                    //                  INSERT INTO sm_Inbound_StockCII (DeliveryNumber, OrderNumber, MaterialNumber, MaterialDescription,SerialNumber,Quantity,InwardDate,SourceLocation,ReceivedBy,Status,RackLocation,Fk_UserCode)
                    //                  VALUES (@DeliveryNumber, @OrderNumber, @MaterialNumber, @MaterialDescription,@SerialNumber,@Quantity,@InwardDate,@SourceLocation,@ReceivedBy,@Status,@RackLocation,@UserCodes);";

                    //using (var command = new SqlCommand(query, connection))
                    //{
                    //	//command.Parameters.AddWithValue("@DeliveryNumber", stock["DeliveryNumber"] ?? DBNull.Value);
                    //	//command.Parameters.AddWithValue("@OrderNumber", stock["OrderNumber"] ?? DBNull.Value);
                    //	//command.Parameters.AddWithValue("@MaterialNumber", stock["MaterialNumber"] ?? DBNull.Value);
                    //	//command.Parameters.AddWithValue("@MaterialDescription", stock["MaterialDescription"] ?? DBNull.Value);
                    //	//command.Parameters.AddWithValue("@SerialNumber", stock["SerialNumber"] ?? DBNull.Value);
                    //	//command.Parameters.AddWithValue("@Quantity", stock["Quantity"] ?? DBNull.Value);
                    //	//command.Parameters.AddWithValue("@InwardDate", stock["InwardDate"] ?? DBNull.Value);
                    //	//command.Parameters.AddWithValue("@SourceLocation", stock["SourceLocation"] ?? DBNull.Value);
                    //	//command.Parameters.AddWithValue("@ReceivedBy", stock["ReceivedBy"] ?? DBNull.Value);
                    //	//command.Parameters.AddWithValue("@Status", stock["Status"] ?? DBNull.Value);
                    //	//command.Parameters.AddWithValue("@RackLocation", stock["RackLocation"] ?? DBNull.Value);
                    //	command.Parameters.Add(new SqlParameter("@DeliveryNumber", SqlDbType.NVarChar)
                    //	{
                    //		Value = (object?)data.DeliveryNumber ?? DBNull.Value
                    //	});

                    //	command.Parameters.Add(new SqlParameter("@OrderNumber", SqlDbType.NVarChar)
                    //	{
                    //		Value = (object?)data.OrderNumber ?? DBNull.Value
                    //	});
                    //	command.Parameters.AddWithValue("@MaterialNumber", data.MaterialNumber);
                    //	command.Parameters.AddWithValue("@MaterialDescription",data.MaterialDescription);
                    //	command.Parameters.AddWithValue("@SerialNumber", stock["SerialNumber"] ?? DBNull.Value);
                    //	command.Parameters.AddWithValue("@Quantity", stock["Quantity"] ?? DBNull.Value);
                    //	command.Parameters.AddWithValue("@InwardDate", data.Inwarddate.HasValue ? data.Inwarddate.Value : (object)DBNull.Value);

                    //	command.Parameters.AddWithValue("@SourceLocation", data.InwardFrom ?? (object)DBNull.Value);
                    //	command.Parameters.AddWithValue("@ReceivedBy", data.ReceivedBy ?? (object)DBNull.Value);
                    //	command.Parameters.AddWithValue("@Status", stock["Status"] ?? DBNull.Value);
                    //	command.Parameters.AddWithValue("@UserCodes", userCodes[0]);
                    //	command.Parameters.Add(new SqlParameter("@RackLocation", SqlDbType.NVarChar)
                    //	{
                    //		Value = (object?)data.RacKLocation ?? DBNull.Value
                    //	});


                    //	await command.ExecuteNonQueryAsync();
                    //}
                }
				//}

				return Ok("Data imported successfully.");
			}
			catch (SqlException sqlEx)
			{
				return StatusCode(400, "Duplicate entry: The Serial number already exists.");
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"An error occurred: {ex.Message}");
			}
			finally
			{
				// Cleanup the uploaded file
				if (System.IO.File.Exists(filePath))
					System.IO.File.Delete(filePath);
			}
		}


		[HttpPost("ImportSingleStockData")]
		public async Task<IActionResult> ImportSingleStockDataF([FromBody] AddSingleStockInward data)
		{
			if (data == null)
			{
				return BadRequest("Invalid request data.");
			}

			try
			{
				var userCode = await _context.Database.SqlQueryRaw<string>("SELECT Pk_UserCode FROM sm_users WHERE loginId = @p0", data.UserName).ToListAsync();

                if (userCode[0] ==  null) // Handle case when user is not found
				{
					return BadRequest("User not found.");
				}

                await _context.Database.ExecuteSqlRawAsync(@"exec Addsinglestock @p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10 ,@p11,@p12", data.UserName, data.DeliveryNumber, data.OrderNumber, data.MaterialNumber, data.MaterialDescription, data.SerialNumber,
				data.Quantity, data.Inwarddate, data.InwardFrom, data.ReceivedBy, data.Status, userCode[0], data.RacKLocation);

                //var connectionString = _configuration.GetConnectionString("MyDBConnection");

                //using (var connection = new SqlConnection(connectionString))
                //{
                //	await connection.OpenAsync();
                //	var query = @"
                //            INSERT INTO sm_Inbound_StockCII 
                //            (DeliveryNumber, OrderNumber, MaterialNumber, MaterialDescription, SerialNumber, Quantity, InwardDate, SourceLocation, ReceivedBy, Status, RackLocation, Fk_UserCode)
                //            VALUES (@DeliveryNumber, @OrderNumber, @MaterialNumber, @MaterialDescription, @SerialNumber, @Quantity, @InwardDate, @SourceLocation, @ReceivedBy, @Status, @RackLocation, @UserCode);";

                //	using (var command = new SqlCommand(query, connection))
                //	{
                //		command.Parameters.AddWithValue("@DeliveryNumber", (object?)data.DeliveryNumber ?? DBNull.Value);
                //		command.Parameters.AddWithValue("@OrderNumber", (object?)data.OrderNumber ?? DBNull.Value);
                //		command.Parameters.AddWithValue("@MaterialNumber", data.MaterialNumber);
                //		command.Parameters.AddWithValue("@MaterialDescription", data.MaterialDescription);
                //		command.Parameters.AddWithValue("@SerialNumber", data.SerialNumber);
                //		command.Parameters.AddWithValue("@Quantity", data.Quantity);
                //		command.Parameters.AddWithValue("@InwardDate", data.Inwarddate.HasValue ? data.Inwarddate.Value : (object)DBNull.Value);

                //		command.Parameters.AddWithValue("@SourceLocation", data.InwardFrom ?? (object)DBNull.Value);
                //		command.Parameters.AddWithValue("@ReceivedBy", data.ReceivedBy ?? (object)DBNull.Value);
                //		command.Parameters.AddWithValue("@Status", data.Status ?? (object)DBNull.Value);
                //		command.Parameters.AddWithValue("@UserCode", userCode[0]);
                //		command.Parameters.AddWithValue("@RackLocation", (object?)data.RacKLocation ?? DBNull.Value);

                //		await command.ExecuteNonQueryAsync();
                //	}
                //}

                return Ok("Data imported successfully.");
			}
			catch (SqlException sqlEx)
			{
				return StatusCode(400, "Duplicate entry: The Serial number already exists.");
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"An error occurred: {ex.Message}");
			}
		}



		[HttpPost("UpdateInbounddata")]
		public async Task<IActionResult> UpdateInbounddata([FromBody]UpdateInboundData data)
		{
			try
			{
				await _context.Database.ExecuteSqlRawAsync(@"exec updateInboundStockCII @p0, @p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10,@p11,@p12", data.userName, data.MaterialNumber, data.SerialNumber, data.ExistSerialNumber,
					data.RackLocation, data.DeliveryNumber, data.OrderNumber, data.InwardDate, data.InwardFrom, data.ReceivedBy, data.QualityChecker,data.QualityCheckerStatus,data.QualityCheckDate);
				return Ok();
			}
			catch (Exception ex)
			{
				// Log the exception or handle it as needed
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}

		[HttpPost("SearchSerialNumber/{username}/{SerialNumber}")]
		public async Task<ActionResult<SerialNumberSearch>> GetSmInboundStockCii(string username, string SerialNumber)
		{
			var customers = _context.StockInboundCIILists.FromSqlRaw(@"exec searchbyserialnumber @p0 , @p1", SerialNumber, username).ToList();
			return Ok(customers);
		}


		// GET: api/SmInboundStockCiis/5
		[HttpGet("{MaterialNumber}")]
        public async Task<ActionResult<SmInboundStockCii>> GetSmInboundStockCii(string MaterialNumber)
        {
			var customers = _context.StockInboundCIILists.FromSqlRaw(@"exec StockCIIListBySerialNumber @p0", MaterialNumber).ToList();
			return Ok(customers);
		}


		[HttpPost("Material")]
		public async Task<IActionResult> AddMaterialNumber([FromBody] AddMaterial data)
		{
			try
			{
				await _context.Database.ExecuteSqlRawAsync(@"exec AddMaterialNumberNew @p0, @p1, @p2", data.userName, data.MaterialNumber, data.MaterialDescription);
				return Ok(); 
			}
			catch (SqlException ex) when (ex.Number == 50001)  // Unique Key Violation
            {
				return StatusCode(400, "Duplicate entry: The material number already exists.");
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"An error occurred: {ex.Message}");
			}
		}
        [HttpPost("update/{ExistMaterialNumber}/{MaterialNumber}/{MaterialDescription}/{userName}")]
        public async Task<IActionResult> UpdateMaterialNumber(string ExistMaterialNumber, string MaterialNumber, string MaterialDescription, string userName)
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync(@"exec updatematerialNumber @p0, @p1, @p2, @p3", userName, ExistMaterialNumber, MaterialNumber, MaterialDescription);
                return Ok();
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

		[HttpPost("{MaterialNumber}/{IsActive}/{userName}")]
		public async Task<ActionResult<StockCiiList>> deleteMaterialNumber(string MaterialNumber, bool IsActive, string userName)
		{
			try
			{

				await _context.Database.ExecuteSqlRawAsync(@"exec deletematerialnumber @p0, @p1, @p2", userName, MaterialNumber, IsActive);
				return Ok();
			}
			catch (Exception ex)
			{
				// Log the exception or handle it as needed
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}
		[HttpPost("serial/{MaterialNumber}/{SerialNumber}")]
        public async Task<ActionResult<SmInboundStockCii>> deleteSerialNumber(string MaterialNumber, string SerialNumber)
        {
            try
            {
                var customers = _context.SmInboundStockCiiDeletes.FromSqlRaw(@"exec sp_deleteserialNumber @p0, @p1", MaterialNumber, SerialNumber).ToList();
                return Ok(customers);

            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

		[HttpPost("UpdateSerialStatus/{MaterialNumber}/{SerialNumber}/{status}/{UserName}")]
		public async Task<IActionResult> UpdateSerialStatus(string MaterialNumber, string SerialNumber, string status, string UserName)
		{
			try
			{
				// Validate input parameters
				if (string.IsNullOrWhiteSpace(MaterialNumber) || string.IsNullOrWhiteSpace(SerialNumber) || string.IsNullOrWhiteSpace(status))
				{
					return BadRequest("MaterialNumber, SerialNumber, and status cannot be empty.");
				}


				var statuss = await _context
		.Database
		.SqlQueryRaw<string>("SELECT status FROM [dbo].[sm_Inbound_StockCII] WHERE SerialNumber = @p0 AND MaterialNumber = @p1", SerialNumber,
			MaterialNumber )
		.ToListAsync();

				// Validate if the status exists and is "Delivered"
				

				if (!string.Equals(statuss[0], "Outward", StringComparison.OrdinalIgnoreCase))
				{
					await _context.Database.ExecuteSqlRawAsync(
					"EXEC sp_updateserialstatus @p0, @p1, @p2,@p3", UserName, MaterialNumber, SerialNumber, status);



					return Ok("Serial status updated successfully.");
					
				}
				else
				{
					return BadRequest("The serial number status should not be 'Outward' before Update.");
				}


				// Execute stored procedure to update serial status
				
			}
			catch (Exception ex)
			{
				// Log the error (use a logging framework like Serilog in production)
				Console.WriteLine($"Error updating serial status: {ex.Message}");

				return StatusCode(500, "An error occurred while processing your request.");
			}
		}


		// PUT: api/SmInboundStockCiis/5
		// To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
		[HttpPut("{id}")]
        public async Task<IActionResult> PutSmInboundStockCii(string id, SmInboundStockCii smInboundStockCii)
        {
            if (id != smInboundStockCii.DeliveryNumber)
            {
                return BadRequest();
            }

            _context.Entry(smInboundStockCii).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SmInboundStockCiiExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Ok();
        }

        // POST: api/SmInboundStockCiis
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<SmInboundStockCii>> PostSmInboundStockCii(SmInboundStockCii smInboundStockCii)
        {
            _context.SmInboundStockCiis.Add(smInboundStockCii);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (SmInboundStockCiiExists(smInboundStockCii.DeliveryNumber))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetSmInboundStockCii", new { id = smInboundStockCii.DeliveryNumber }, smInboundStockCii);
        }

        // DELETE: api/SmInboundStockCiis/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSmInboundStockCii(string id)
        {
            var smInboundStockCii = await _context.SmInboundStockCiis.FindAsync(id);
            if (smInboundStockCii == null)
            {
                return NotFound();
            }

            _context.SmInboundStockCiis.Remove(smInboundStockCii);
            await _context.SaveChangesAsync();

            return Ok();
        }

        private bool SmInboundStockCiiExists(string id)
        {
            return _context.SmInboundStockCiis.Any(e => e.DeliveryNumber == id);
        }
    }
}
