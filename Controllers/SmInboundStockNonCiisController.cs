using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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
	public class SmInboundStockNonCiisController : ControllerBase
	{
		private readonly MydbContext _context;
		private readonly IWebHostEnvironment _environment;
		private readonly IConfiguration _configuration;

		public SmInboundStockNonCiisController(IWebHostEnvironment environment, IConfiguration configuration, MydbContext context)
		{
			_environment = environment;
			_configuration = configuration;
			_context = context;
		}

		// GET: api/SmInboundStockNonCiis
		[HttpGet("GetSmInboundNonStockCiis/{UserName}")]
		public async Task<ActionResult> GetSmInboundNonStockCiis(string UserName)
		{
			var customers = _context.NonStockCIILists.FromSqlRaw(@"exec Non_StockCIIList @p0", UserName).ToList();
			return Ok(customers);

		}

		[HttpPost("NonStockCIIMaterial")]
		public async Task<IActionResult> AddMaterialNumber([FromBody] AddMaterial data)
		{
			try
			{
				await _context.Database.ExecuteSqlRawAsync(@"exec AddNonStockCII_MaterialNumber @p0, @p1, @p2", data.userName, data.MaterialNumber, data.MaterialDescription);
				return Ok();
			}
			catch (SqlException ex) when (ex.Number == 2627) // Unique Key Violation
			{
				return StatusCode(400, "Duplicate entry: The material number already exists.");
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"An error occurred: {ex.Message}");
			}
		}

		[HttpGet("GetInwardNonStockCiis/{MaterialNumber}")]
		public async Task<ActionResult> GetInwardNonStockCiis(string MaterialNumber)
		{
			var customers = _context.NonStockInwardLists.FromSqlRaw(@"exec sp_getInboundStock_NonCII @p0", MaterialNumber).ToList();
			return Ok(customers);

		}


		[HttpPost("AddNonStockInbounddata")]
		public async Task<IActionResult> AddNonStockInbounddata([FromBody] AddNonStockInward data)
		{
			try
			{
				await _context.Database.ExecuteSqlRawAsync(@"exec Sp_AddInboundStock_NonCII @p0, @p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9", data.DeliveryNumber, data.OrderNumber, data.MaterialNumber,
					data.MaterialDescription, data.Inwarddate, data.InwardFrom, data.ReceivedBy, data.RacKLocation, data.QuantityReceived, data.UserName);
				return Ok();
			}
			catch (Exception ex)
			{
				// Log the exception or handle it as needed
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}

		[HttpPost("DeleteNonStockInbounddata/{MaterialNumber}/{DeliveryNumber}/{InboundStockNonCIIKey}/{name}")]
		public async Task<IActionResult> DeleteNonStockInbounddata(string MaterialNumber, string DeliveryNumber, string InboundStockNonCIIKey, string name)
		{
			try
			{
				await _context.Database.ExecuteSqlRawAsync(@"exec Sp_DeleteNonStockCII @p0, @p1,@p2,@p3", name,MaterialNumber, DeliveryNumber, InboundStockNonCIIKey);
				return Ok();
			}
			catch (Exception ex)
			{
				// Log the exception or handle it as needed
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}

		[HttpPost("UpdateNonStockInbounddata")]
		public async Task<IActionResult> UpdateNonStockInbounddata([FromBody] UpdateNonStockInward data)
		{
			try
			{
				await _context.Database.ExecuteSqlRawAsync(@"exec Sp_UpdateInboundStock_NonCII @p0, @p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10",data.UserName, data.DeliveryNumber, data.OrderNumber, data.MaterialNumber,
					data.ExistDeliveryNumber, data.ExistOrderNumber, data.Inwarddate, data.InwardFrom, data.ReceivedBy, data.RacKLocation, data.QuantityReceived);
				return Ok();
			}
			catch (Exception ex)
			{
				// Log the exception or handle it as needed
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}


		[HttpPost("AddNonStockOutbounddata")]
		public async Task<IActionResult> AddNonStockOutbounddata([FromBody] AddOutBoundNonStockCII data)
		{
			try
			{
				// Fetch the inbound stock details
				var customers = await _context.SmOutBounddatas
					.FromSqlRaw(
						@"SELECT * FROM sm_InboundStock_NonCII 
                WHERE materialnumber = @p0 AND deliverynumber = @p1  ",
						data.MaterialNumber, data.DeliveryNumber
					).ToListAsync();

				// Check if any data was retrieved
				if (customers == null || customers.Count == 0)
				{
					return NotFound("No matching inbound stock record found.");
				}

				var customer = customers[0]; // Assuming you want to use the first row

				// Validate if the delivered quantity does not exceed the available inbound quantity
				int totalInboundQuantity = customers.Sum(c => c.DeliveredQuantity );

				if (totalInboundQuantity < data.DeliveredQuantity)
				{
					return StatusCode(500, "Delivered quantity exceeds in-stock quantity. Please adjust the delivered quantity.");
				}

				// Deduct the requested delivered quantity from the available inbound stock
				int remainingQuantityToWithdraw = data.DeliveredQuantity ?? 0;

				foreach (var stockItem in customers)
				{
					if (stockItem.DeliveredQuantity <= remainingQuantityToWithdraw)
					{
						// Deduct the full quantity from this row
						remainingQuantityToWithdraw -= stockItem.DeliveredQuantity;
						stockItem.DeliveredQuantity = 0;  // Set quantity to 0
					}
					else
					{
						// Deduct only the required quantity from this row
						stockItem.DeliveredQuantity -= remainingQuantityToWithdraw;
						remainingQuantityToWithdraw = 0;
						break;  // No further deduction is needed
					}
				}

				// Add to outbound stock
				await _context.Database.ExecuteSqlRawAsync(
					@"EXEC Sp_AddOutboundStock_NonCII 
                @p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9,@p10",
					data.UserName,data.DeliveryNumber, data.OrderNumber, data.MaterialNumber,
					data.MaterialDescription, data.OutboundDate, data.ReceiverName,
					data.TargetLocation, data.DeliveredQuantity, data.SentBy, data.DeliveryNumber_inbound
				);

				// Update the inbound stock table
				foreach (var stockItem in customers)
				{
					await _context.Database.ExecuteSqlRawAsync(
						@"UPDATE sm_InboundStock_NonCII 
                SET DeliveredQuantity = @p0 
                WHERE materialnumber = @p1 AND deliverynumber = @p2 and InboundStockNonCIIKey= @p3",
						stockItem.DeliveredQuantity, data.MaterialNumber, stockItem.DeliveryNumber, stockItem.InboundStockNonCIIKey
					);
				}

				return Ok("Outbound data added successfully.");
			}
			catch (Exception ex)
			{
				// Log the exception for debugging
				Console.WriteLine($"Error: {ex.Message}");
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}




		[HttpGet("DeliveredDataList/{MaterialNumber}")]
		public async Task<ActionResult> GetDeliveredDataList(string MaterialNumber)
		{
			var customers = _context.GetNonStockDeliveredDatas
    .FromSqlRaw("SELECT * FROM sm_OutboundStock_NonCII WHERE materialnumber = @MaterialNumber AND IsActive <> 0",
				new SqlParameter("@MaterialNumber", MaterialNumber))
	.ToList();
			return Ok(customers);

		}



		[HttpPost("DeleteNonStockDeliverdata/{MaterialNumber}/{DeliveryNumber}/{OutboundStockNonCIIKey}/{UserName}")]
		public async Task<IActionResult> DeleteNonStockDeliverdata(string MaterialNumber, string DeliveryNumber, string OutboundStockNonCIIKey,string Username)
		{
			if (string.IsNullOrWhiteSpace(MaterialNumber) || string.IsNullOrWhiteSpace(DeliveryNumber))
			{
				return BadRequest("MaterialNumber and DeliveryNumber are required.");
			}

			try
			{
				// Fetch outbound stock record to get the delivered quantity
				var outboundStock = await _context.GetNonStockDeliveredDatas
					.FromSqlRaw(
						"SELECT * FROM sm_OutboundStock_NonCII WHERE materialnumber = @MaterialNumber AND deliverynumber = @DeliveryNumber AND OutboundStockNonCIIKey = @OutboundStockNonCIIKey AND IsActive <> 0",
						new SqlParameter("@MaterialNumber", MaterialNumber),
						new SqlParameter("@DeliveryNumber", DeliveryNumber),
						new SqlParameter("@OutboundStockNonCIIKey", OutboundStockNonCIIKey)
					).FirstOrDefaultAsync();

				if (outboundStock == null)
				{
					return NotFound("No matching outbound stock record found.");
				}

				int deliveredQuantityToReturn = outboundStock.DeliveredQuantity;

				// Fetch inbound stock records to return the quantity
				var inboundStocks = await _context.SmOutBounddatas
					.FromSqlRaw(
						@"SELECT * FROM sm_InboundStock_NonCII 
                  WHERE materialnumber = @p0 AND deliverynumber = @p1",
						new SqlParameter("@p0", MaterialNumber),
						new SqlParameter("@p1", DeliveryNumber)
					)
					.OrderBy(i => i.InboundStockNonCIIKey) // Order for proper reinsertion
					.ToListAsync();

				if (inboundStocks == null || inboundStocks.Count == 0)
				{
					return NotFound("No matching inbound stock records found.");
				}

				// Return the delivered quantity to inbound stock
				int remainingToAdd = deliveredQuantityToReturn;
				foreach (var stock in inboundStocks)
				{
					stock.DeliveredQuantity += remainingToAdd;
					break; // Update the first available record
				}

				// Call stored procedure to delete the outbound stock record
				await _context.Database.ExecuteSqlRawAsync(
					@"EXEC Sp_DeleteNonStockDeliverCII @p0, @p1, @p2,@p3",
                    new SqlParameter("@p0", Username),
                    new SqlParameter("@p1", MaterialNumber),
					new SqlParameter("@p2", DeliveryNumber),
					new SqlParameter("@p3", OutboundStockNonCIIKey)
				);

				// Update inbound stock records
				foreach (var stock in inboundStocks)
				{
					await _context.Database.ExecuteSqlRawAsync(
						@"UPDATE sm_InboundStock_NonCII 
                  SET DeliveredQuantity = @p0 
                  WHERE materialnumber = @p1 AND deliverynumber = @p2 AND InboundStockNonCIIKey = @p3",
						new SqlParameter("@p0", stock.DeliveredQuantity),
						new SqlParameter("@p1", MaterialNumber),
						new SqlParameter("@p2", DeliveryNumber),
						new SqlParameter("@p3", stock.InboundStockNonCIIKey)
					);
				}

				return Ok("Non-stock delivery data deleted and inbound stock updated successfully.");
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"An error occurred: {ex.Message}");
			}
		}





		[HttpPost("UpdateNonStockDeliverdata")]
		public async Task<IActionResult> UpdateNonStockDeliverdata([FromBody] UpdateNonStockDeliverData data)
		{
			try
			{
				if (data == null)
				{
					return BadRequest("Request data is null.");
				}

				// Retrieve outbound stock record to get existing delivered quantity
				var outboundStock = await _context.GetNonStockDeliveredDatas
					.FromSqlRaw(@"SELECT * FROM sm_OutboundStock_NonCII 
                         WHERE materialnumber = @p0 AND deliverynumber = @p1 and OutboundStockNonCIIKey= @p2 ",
								 data.MaterialNumber, data.DeliveryNumber, data.OutboundStockNonCIIKey)
					.FirstOrDefaultAsync();

				if (outboundStock == null)
				{
					return NotFound("No matching outbound stock record found.");
				}

				int existingOutboundQuantity = data.ExistDeliveredQuantity ?? 0;
				int quantityDifference = (data.DeliveredQuantity ?? 0)- existingOutboundQuantity;

				// Retrieve inbound stock data
				var inboundStocks = await _context.SmOutBounddatas
					.FromSqlRaw(@"SELECT * FROM sm_InboundStock_NonCII 
                         WHERE materialnumber = @p0 AND deliverynumber = @p1 ",
								 data.MaterialNumber, data.DeliveryNumber)
					.OrderBy(i => i.InboundStockNonCIIKey) // Order to manage proper deductions
					.ToListAsync();

				if (inboundStocks == null || inboundStocks.Count == 0)
				{
					return NotFound("No matching inbound stock record found.");
				}

				if (quantityDifference > 0)
				{
					// Increase outbound quantity: Deduct from inbound stock
					int remainingToDeduct = quantityDifference;

					foreach (var stock in inboundStocks)
					{
						if (stock.DeliveredQuantity >= remainingToDeduct)
						{
							stock.DeliveredQuantity -= remainingToDeduct;
							remainingToDeduct = 0;
							break;
						}
						else
						{
							remainingToDeduct -= stock.DeliveredQuantity;
							stock.DeliveredQuantity = 0;
						}
					}

					if (remainingToDeduct > 0)
					{
						return BadRequest("Not enough available stock to fulfill the increase in outbound quantity.");
					}
				}
				else if (quantityDifference < 0)
				{
					// Decrease outbound quantity: Return to inbound stock
					int remainingToAdd = -quantityDifference;

					foreach (var stock in inboundStocks)
					{
						stock.DeliveredQuantity += remainingToAdd;
						break; // Since we are returning stock, update the first available entry
					}
				}

				// Update outbound stock
				await _context.Database.ExecuteSqlRawAsync(
					@"EXEC Sp_UpdateOutboundStock_NonCII 
              @p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9,@p10,@p11",
					data.UserName,
					data.DeliveryNumber,
					data.OrderNumber,
					data.ExistDeliveryNumber,
					data.ExistOrderNumber,
					data.MaterialNumber,
					data.OutboundDate,
					data.ReceiverName,
					data.TargetLocation,
					data.DeliveredQuantity,
					data.SentBy,
					data.OutboundStockNonCIIKey
				);

				// Update inbound stock records
				foreach (var stock in inboundStocks)
				{
					await _context.Database.ExecuteSqlRawAsync(
						@"UPDATE sm_InboundStock_NonCII 
                  SET DeliveredQuantity = @p0 
                  WHERE materialnumber = @p1 AND deliverynumber = @p2 AND InboundStockNonCIIKey = @p3",
						stock.DeliveredQuantity, data.MaterialNumber, data.DeliveryNumber, stock.InboundStockNonCIIKey
					);
				}

				return Ok("Inbound and outbound stock updated successfully.");
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"An error occurred: {ex.Message}");
			}
		}





		[HttpPost("AddNonStockReturndata")]
		public async Task<IActionResult> AddNonStockReturndata([FromBody] AddNonStockReturnData data)
		{
			try
			{
				await _context.Database.ExecuteSqlRawAsync(@"exec sp_AddReturnNonStockData @p0, @p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8", data.OrderNumber, data.MaterialNumber,
					data.ReturnLocation, data.Returndate, data.ReturnQuantity, data.ReceivedBy, data.RackLocation, data.ReturnType, data.Reason);
				return Ok();
			}
			catch (Exception ex)
			{
				// Log the exception or handle it as needed
				return StatusCode(500, "An error occurr" +
					"ed while processing your request.");
			}
		}

		[HttpPost("UpdateNonStockReturndata")]
		public async Task<IActionResult> UpdateNonStockReturndata([FromBody] UpdateNonStockRetundata data)
		{
			try
			{
				await _context.Database.ExecuteSqlRawAsync(@"exec sp_UpdateReturnNonStockData @p0, @p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9", data.OrderNumber, data.ExistOrderNumber, data.MaterialNumber,
					data.ReturnLocation, data.Returndate, data.ReturnQuantity, data.ReceivedBy, data.RackLocation, data.ReturnType, data.Reason);
				return Ok();
			}
			catch (Exception ex)
			{
				// Log the exception or handle it as needed
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}


		[HttpPost("UpdateNonStockReturnType/{MaterialNumber}/{OrderNumber}")]
		public async Task<IActionResult> UpdateNonStockReturnType(string MaterialNumber, string OrderNumber)
		{
			try
			{
				await _context.Database.ExecuteSqlRawAsync(@"exec sp_UpdateNonStockReturnType @p0, @p1", MaterialNumber, OrderNumber);
				return Ok();
			}
			catch (Exception ex)
			{
				// Log the exception or handle it as needed
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}
		[HttpPost("DeleteNonStockReturnData/{MaterialNumber}/{OrderNumber}")]
		public async Task<IActionResult> DeleteNonStockReturnData(string MaterialNumber, string OrderNumber)
		{
			try
			{
				await _context.Database.ExecuteSqlRawAsync(@"exec sp_DeleteNonStockReturndata @p0, @p1", MaterialNumber, OrderNumber);
				return Ok();
			}
			catch (Exception ex)
			{
				// Log the exception or handle it as needed
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}


		[HttpPost("GetNonStockReturnData/{MaterialNumber}")]
		public async Task<IActionResult> GetNonStockReturnData(string MaterialNumber)
		{
			try
			{
				var customers = _context.NonStockReturnCiis.FromSqlRaw(@"exec sp_GateNonStockReturnData @p0", MaterialNumber);
				return Ok(customers);
			}
			catch (Exception ex)
			{
				// Log the exception or handle it as needed
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}

		[HttpPost("GetNonStockUsedData/{MaterialNumber}")]
		public async Task<IActionResult> GetNonStockUsedData(string MaterialNumber)
		{
			try
			{
				var customers = _context.GetUsedStocks.FromSqlRaw(@"exec Sp_GetUsedStock_NonCII @p0", MaterialNumber);
				return Ok(customers);
			}
			catch (Exception ex)
			{
				// Log the exception or handle it as needed
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}

		[HttpPost("AddNonStockUsedData")]
		public async Task<IActionResult> AddNonStockUsedData([FromBody] AddUsedStock data)
		{
			try
			{
				await _context.Database.ExecuteSqlRawAsync(@"exec Sp_AddUsedStock_NonCII @p0,@p1,@p2,@p3,@p4", data.OrderNumber, data.MaterialNumber, data.ReturnDate, data.ReturnLocation, data.ItemQuantity);
				return Ok();
			}
			catch (Exception ex)
			{
				// Log the exception or handle it as needed
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}

		[HttpPost("UpdateNonStockUsedData")]
		public async Task<IActionResult> UpdateNonStockUsedData([FromBody] UpdateUsedStock data)
		{
			try
			{
				await _context.Database.ExecuteSqlRawAsync(@"exec Sp_UpdateUsedStock_NonCII @p0,@p1,@p2,@p3,@p4,@p5", data.OrderNumber,data.ExistOrderNumber, data.MaterialNumber, data.ReturnDate, data.ReturnLocation, data.ItemQuantity);
				return Ok();
			}
			catch (Exception ex)
			{
				// Log the exception or handle it as needed
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}

		[HttpPost("DeleteNonStockUsedData/{MaterialNumber}/{OrderNumber}")]
		public async Task<IActionResult> DeleteNonStockUsedData(string MaterialNumber, string OrderNumber)
		{
			try
			{
				await _context.Database.ExecuteSqlRawAsync(@"exec Sp_DeleteUsedStock_NonCII @p0,@p1", OrderNumber, MaterialNumber);
				return Ok();
			}
			catch (Exception ex)
			{
				// Log the exception or handle it as needed
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}


		[HttpPost("DashBoard/{UserName}")]
		public async Task<IActionResult> DashBoard(string UserName)
		{
			try
			{
				var CIICount = _context.DashboardLists.FromSqlRaw(@"exec dashboard_cii_stock @p0", UserName);
				var NonCIICount = _context.DashboardLists.FromSqlRaw(@"exec dashboard_Non_StockCIIList @p0", UserName);
				var DeliveryReturnCount = _context.DashboardDeliveryCounts.FromSqlRaw(@"exec dashboard_delivery_return_count @p0", UserName);
				return Ok(new
				{
					CIICounts= CIICount,
					NonCIICounts= NonCIICount,
					DeliveryReturnCounts= DeliveryReturnCount

				});
			}
			catch (Exception ex)
			{
				// Log the exception or handle it as needed
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}

		[HttpPost("AnalyticsCII/{MaterialNumber}")]
		public async Task<IActionResult> AnalyticsCII(string MaterialNumber)
		{
			try
			{
				
				var DeliveryReturnCount = _context.AnalyticsDashboards.FromSqlRaw(@"exec AnalyticsCount_CII @p0", MaterialNumber);
				return Ok(DeliveryReturnCount);
			}
			catch (Exception ex)
			{
				// Log the exception or handle it as needed
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}

		[HttpPost("AnalyticsNonCII/{MaterialNumber}")]
		public async Task<IActionResult> AnalyticsNonCII(string MaterialNumber)
		{
			try
			{

				var DeliveryReturnCount = _context.AnalyticsDashboards.FromSqlRaw(@"exec AnalyticsCount_NonCII @p0", MaterialNumber);
				return Ok(DeliveryReturnCount);
			}
			catch (Exception ex)
			{
				// Log the exception or handle it as needed
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}


		[HttpPost("DashBoardCharts/{Username}")]
		public async Task<IActionResult> DashBoardCharts(string Username)
		{
			try
			{

				var CIIInwardCount = _context.Dashboardcharts.FromSqlRaw(@" exec sp_CIIInwardCount @p0", Username);
				var NonCIIInwardCount = _context.Dashboardcharts.FromSqlRaw(@"exec sp_NonCIIInwardCount @p0", Username);
				var CIIDeliveryCount = _context.Dashboardcharts.FromSqlRaw(@" exec sp_CIIDeliveryCount @p0", Username);
				var NonCIIDeliveryCount = _context.Dashboardcharts.FromSqlRaw(@"exec sp_NonCIIDeliveryCount @p0", Username);

				return Ok(new
				{
					CIIInwardCounts = CIIInwardCount,
					NonCIIInwardCounts = NonCIIInwardCount,
					CIIDeliveryCounts = CIIDeliveryCount,
					NonCIIDeliveryCounts = NonCIIDeliveryCount
				});
			}
			catch (Exception ex)
			{
				// Log the exception or handle it as needed
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}
















































































		// GET: api/SmInboundStockNonCiis/5
		[HttpGet("{id}")]
        public async Task<ActionResult<SmInboundStockNonCii>> GetSmInboundStockNonCii(string id)
        {
            var smInboundStockNonCii = await _context.SmInboundStockNonCiis.FindAsync(id);

            if (smInboundStockNonCii == null)
            {
                return NotFound();
            }

            return smInboundStockNonCii;
        }

        // PUT: api/SmInboundStockNonCiis/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSmInboundStockNonCii(string id, SmInboundStockNonCii smInboundStockNonCii)
        {
            if (id != smInboundStockNonCii.DeliveryNumber)
            {
                return BadRequest();
            }

            _context.Entry(smInboundStockNonCii).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SmInboundStockNonCiiExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/SmInboundStockNonCiis
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<SmInboundStockNonCii>> PostSmInboundStockNonCii(SmInboundStockNonCii smInboundStockNonCii)
        {
            _context.SmInboundStockNonCiis.Add(smInboundStockNonCii);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (SmInboundStockNonCiiExists(smInboundStockNonCii.DeliveryNumber))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetSmInboundStockNonCii", new { id = smInboundStockNonCii.DeliveryNumber }, smInboundStockNonCii);
        }

        // DELETE: api/SmInboundStockNonCiis/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSmInboundStockNonCii(string id)
        {
            var smInboundStockNonCii = await _context.SmInboundStockNonCiis.FindAsync(id);
            if (smInboundStockNonCii == null)
            {
                return NotFound();
            }

            _context.SmInboundStockNonCiis.Remove(smInboundStockNonCii);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool SmInboundStockNonCiiExists(string id)
        {
            return _context.SmInboundStockNonCiis.Any(e => e.DeliveryNumber == id);
        }
    }
}
