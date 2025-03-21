﻿using System;
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

		[HttpPost("NonStockCIIMaterial/{MaterialNumber}/{MaterialDescription}")]
		public async Task<IActionResult> AddMaterialNumber(string MaterialNumber, string MaterialDescription)
		{
			try
			{
				await _context.Database.ExecuteSqlRawAsync(@"exec AddNonStockCII_MaterialNumber @p0, @p1", MaterialNumber, MaterialDescription);
				return Ok();
			}
			catch (Exception ex)
			{
				// Log the exception or handle it as needed
				return StatusCode(500, "An error occurred while processing your request.");
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

		[HttpPost("DeleteNonStockInbounddata/{MaterialNumber}/{DeliveryNumber}/{OrderNumber}")]
		public async Task<IActionResult> DeleteNonStockInbounddata(string MaterialNumber, string DeliveryNumber, string OrderNumber)
		{
			try
			{
				await _context.Database.ExecuteSqlRawAsync(@"exec Sp_DeleteNonStockCII @p0, @p1,@p2", MaterialNumber, DeliveryNumber, OrderNumber);
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
				await _context.Database.ExecuteSqlRawAsync(@"exec Sp_UpdateInboundStock_NonCII @p0, @p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9", data.DeliveryNumber, data.OrderNumber, data.MaterialNumber,
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
				var customers = await _context.SmInboundStockNonCiis
					.FromSqlRaw(
						@"SELECT * FROM sm_InboundStock_NonCII 
				  WHERE materialnumber = @p0 AND deliverynumber = @p1 AND OrderNumber= @p2 ",
						data.MaterialNumber, data.DeliveryNumber , data.OrderNumber
					).ToListAsync();

				// Check if any data was retrieved
				if (customers == null || customers.Count == 0)
				{
					return NotFound("No matching inbound stock record found.");
				}

				var customer = customers[0];

				// Validate the delivered quantity
				if (customer.DeliveredQuantity < data.DeliveredQuantity)
				{
					return StatusCode(500, "Delivered quantity exceeds in-stock quantity. Please adjust the delivered quantity.");
				}

				// Calculate the new inbound quantity
				var inboundQuantity = customer.DeliveredQuantity - data.DeliveredQuantity;

				// Add to outbound stock
				await _context.Database.ExecuteSqlRawAsync(
					@"EXEC Sp_AddOutboundStock_NonCII 
			  @p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9",
					data.DeliveryNumber, data.OrderNumber, data.MaterialNumber,
					data.MaterialDescription, data.OutboundDate, data.ReceiverName,
					data.TargetLocation, data.DeliveredQuantity, data.SentBy, data.DeliveryNumber_inbound
				);

				// Update inbound stock
				await _context.Database.ExecuteSqlRawAsync(
					@"UPDATE sm_InboundStock_NonCII 
			  SET DeliveredQuantity = @p0 
			  WHERE materialnumber = @p1 AND deliverynumber = @p2 AND OrderNumber= @p3",
					inboundQuantity, data.MaterialNumber, data.DeliveryNumber , data.OrderNumber
				);

				return Ok("Outbound data added successfully.");
			}
			catch (Exception ex)
			{
				// Log the exception for debugging
				// Consider using a logging framework like Serilog or NLog
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

		[HttpPost("DeleteNonStockDeliverdata/{MaterialNumber}/{DeliveryNumber}/{OrderNumber}")]
		public async Task<IActionResult> DeleteNonStockDeliverdata(string MaterialNumber, string DeliveryNumber, string OrderNumber)
		{
			if (string.IsNullOrWhiteSpace(MaterialNumber) || string.IsNullOrWhiteSpace(DeliveryNumber) || string.IsNullOrWhiteSpace(OrderNumber))
			{
				return BadRequest("MaterialNumber, DeliveryNumber, and OrderNumber are required.");
			}

			try
			{
				// Fetch outbound stock
				var customers = await _context.SmOutboundStockNonCiis
					.FromSqlRaw(
						"SELECT * FROM sm_OutboundStock_NonCII WHERE materialnumber = @MaterialNumber AND IsActive <> 0",
						new SqlParameter("@MaterialNumber", MaterialNumber)
					).ToListAsync();

				if (customers == null || !customers.Any())
				{
					return NotFound("No matching outbound stock records found.");
				}

				// Fetch inbound stock
				var inboundStock = await _context.SmInboundStockNonCiis
					.FromSqlRaw(
						@"SELECT * FROM sm_InboundStock_NonCII 
                  WHERE materialnumber = @p0 AND deliverynumber = @p1 AND ordernumber = @p2",
						new SqlParameter("@p0", MaterialNumber),
						new SqlParameter("@p1", DeliveryNumber),
						new SqlParameter("@p2", OrderNumber)
					).ToListAsync();

				if (inboundStock == null || !inboundStock.Any())
				{
					return NotFound("No matching inbound stock records found.");
				}

				// Calculate new quantity
				var totalQuantity = customers[0].DeliveredQuantity + inboundStock[0].DeliveredQuantity;

				// Call stored procedure to delete the non-stock delivery data
				await _context.Database.ExecuteSqlRawAsync(
					@"exec Sp_DeleteNonStockDeliverCII @p0, @p1, @p2",
					new SqlParameter("@p0", MaterialNumber),
					new SqlParameter("@p1", DeliveryNumber),
					new SqlParameter("@p2", OrderNumber)
				);

				// Update inbound stock's delivered quantity
				await _context.Database.ExecuteSqlRawAsync(
					@"UPDATE sm_InboundStock_NonCII 
              SET DeliveredQuantity = @p0 
              WHERE materialnumber = @p1 AND deliverynumber = @p2 AND ordernumber = @p3",
					new SqlParameter("@p0", totalQuantity),
					new SqlParameter("@p1", MaterialNumber),
					new SqlParameter("@p2", DeliveryNumber),
					new SqlParameter("@p3", OrderNumber)
				);

				return Ok("Non-stock delivery data deleted and inbound stock updated successfully.");
			}
			catch (Exception ex)
			{
				// Log the exception as needed
				// Example: _logger.LogError(ex, "Error deleting non-stock delivery data");
				return StatusCode(500, $"An error occurred: {ex.Message}");
			}
		}


		[HttpPost("UpdateNonStockDeliverdata")]
		public async Task<IActionResult> UpdateNonStockDeliverdata([FromBody] UpdateNonStockDeliverData data)
		{
			try
			{
				// Validate input data
				if (data == null)
				{
					return BadRequest("Request data is null.");
				}

				// Retrieve the inbound stock record
				var customers = await _context.SmInboundStockNonCiis
					.FromSqlRaw(
						@"SELECT * FROM sm_InboundStock_NonCII 
                WHERE materialnumber = @p0 AND deliverynumber = @p1 and ordernumber =@p2",
						data.MaterialNumber, data.DeliveryNumber , data.OrderNumber
					).ToListAsync();

				// Check if any data was retrieved
				if (customers == null || customers.Count == 0)
				{
					return NotFound("No matching inbound stock record found.");
				}

				// Get the first record (assuming unique material and delivery number)
				var customer = customers.FirstOrDefault();
				if (customer == null)
				{
					return NotFound("Inbound stock record not found.");
				}

				// Initialize variables
				int quantity = 0;
				int inboundQuantity = customer.DeliveredQuantity ?? 0;

				// Adjust quantities if DeliveredQuantity has changed
				if (data.DeliveredQuantity != data.ExistDeliveredQuantity)
				{
					if (data.DeliveredQuantity < data.ExistDeliveredQuantity)
					{
						// DeliveredQuantity is being reduced
						quantity = data.ExistDeliveredQuantity - data.DeliveredQuantity;

						if (inboundQuantity < quantity)
						{
							return BadRequest("Delivered quantity exceeds available in-stock quantity. Please adjust the delivered quantity.");
						}

						// Reduce the inbound quantity
						inboundQuantity += quantity;
					}
					else
					{
						// DeliveredQuantity is being increased
						quantity = data.DeliveredQuantity - data.ExistDeliveredQuantity;

						// Increase the inbound quantity
						inboundQuantity -= quantity;
					}
					await _context.Database.ExecuteSqlRawAsync(
					@"UPDATE sm_InboundStock_NonCII 
              SET DeliveredQuantity = @p0 
              WHERE materialnumber = @p1 AND deliverynumber = @p2 and ordernumber= @p3",
					inboundQuantity, data.MaterialNumber, data.DeliveryNumber ,data.OrderNumber
				);
				}

				// Execute stored procedure to update outbound stock
				await _context.Database.ExecuteSqlRawAsync(
					@"exec Sp_UpdateOutboundStock_NonCII 
              @p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9",
					data.DeliveryNumber,
					data.OrderNumber,
					data.ExistDeliveryNumber,
					data.ExistOrderNumber,
					data.MaterialNumber,
					data.OutboundDate,
					data.ReceiverName,
					data.TargetLocation,
					data.DeliveredQuantity,
					data.SentBy
				);

				// Update the inbound stock record
				

				// Return success response
				return Ok("Inbound stock and outbound stock updated successfully.");
			}
			catch (Exception ex)
			{
				// Log the exception for debugging purposes
				// Consider injecting a logger (e.g., ILogger) for proper logging
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
