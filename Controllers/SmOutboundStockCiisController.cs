using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagementWebApi.Models;

namespace StockManagementWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SmOutboundStockCiisController : ControllerBase
    {
        private readonly MydbContext _context;

        public SmOutboundStockCiisController(MydbContext context)
        {
            _context = context;
        }

        // GET: api/SmOutboundStockCiis
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SmOutboundStockCii>>> GetSmOutboundStockCiis()
        {
            return await _context.SmOutboundStockCiis.ToListAsync();
        }

		[HttpPost("CollectionPointUpdate")]
		public async Task<ActionResult> CollectionPointUpdate([FromBody] CollectionPointDetail data)
		{
			try
			{
				var sql = @"EXEC UpdateCollectionPointDetails 
		           @MaterialNumber = {0}, 
		           @SerialNumber = {1}, 
		           @CollectionPointStatus = {2}, 
		           @CollectionPointDate = {3}, 
		           @CollectionPointerName = {4},
                    @RackLocation={4}";

				await _context.Database.ExecuteSqlRawAsync(sql,
					data.MaterialNumber,
					data.SerialNumber,
					data.CollectionPointStatus,
					data.CollectionPointDate,
					data.CollectionPointerName,
					data.RackLocation);

				return Ok();
			}
			catch (Exception ex)
			{
				// Log the exception or handle it as needed
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}

		[HttpPost("{MaterialNumber}/{SerialNumber}/{OrderNumber}")]
		public async Task<ActionResult> outboundstockList(string MaterialNumber, string SerialNumber, string OrderNumber)
		{
			try
			{
                var CIIdata = _context.InboundCIILists.FromSqlRaw(@"exec sp_inboundstockList @p0, @p1, @p2", MaterialNumber, SerialNumber, OrderNumber).ToList();
				var Deliverydata = _context.OutboundDataLists.FromSqlRaw(@"exec sp_outboundstockList @p0, @p1", MaterialNumber, SerialNumber).ToList();
				var Inbounddata = _context.ReturnStockDatas.FromSqlRaw(@"exec deliverystockCII @p0,@p1", MaterialNumber, SerialNumber).ToList();

				return Ok(
                    new
                    {
						CIIData= CIIdata,
						InboundData = Inbounddata,
						DeliveryData= Deliverydata
					});

			}
			catch (Exception ex)
			{
				// Log the exception or handle it as needed
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}
		[HttpPost("AddOutboundData")]
		public async Task<ActionResult> AddOutboundData([FromBody] AddDeliveryData data)
		{
			if (data == null)
			{
				return BadRequest("Invalid request data.");
			}

			try
			{
				// Validate if the serial number and material number exist
				var status = await _context
		.Database
		.SqlQueryRaw<string>("SELECT status FROM [dbo].[sm_Inbound_StockCII] WHERE SerialNumber = @p0 AND MaterialNumber = @p1",
			data.SerialNumber, data.MaterialNumber)
		.ToListAsync();


				if (new[] { "Outward", "Defective", "Damaged","BreakFix" }.Contains(status[0], StringComparer.OrdinalIgnoreCase))
				{
					return StatusCode(400, "The serial number is already Outward/Defective");
				}

				// Execute stored procedure to add inbound stock
				await _context.Database.ExecuteSqlRawAsync(
					"EXEC AddInboundStockCII @p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9",
					data.DeliveryNumber, data.MaterialNumber, data.SerialNumber, data.MaterialDescription, data.OrderNumber,
					data.OutBounddate, data.TargetLocation, data.SentBy, data.Fk_Inbound_StockCII_DeliveryNumber, data.ReceiverName);

				// Update the material type to 'Not Available'
				await _context.Database.ExecuteSqlRawAsync(
					"UPDATE sm_Inbound_StockCII SET Status = 'Outward' WHERE serialNumber = @p0 AND materialNumber = @p1",
					data.SerialNumber, data.MaterialNumber);

				return Ok("Outward data added successfully.");
			}
			catch (DbUpdateException dbEx)
			{
				// Log database exceptions
				
				return StatusCode(500, "A database error occurred while processing your request.");
			}
			catch (Exception ex)
			{
				
				return StatusCode(500, "An unexpected error occurred while processing your request.");
			}
		}


		[HttpPost("UpdatedeliveryData")]
		public async Task<ActionResult> UpdatedeliveryData([FromBody] UpdatedeliveryDataList data)
		{
             
			try
			{
				
				await _context.Database.ExecuteSqlRawAsync(@"exec updatedeliverydata @p0, @p1,@p2,@p3,@p4,@p5,@p6,@p7",
				  data.MaterialNumber, data.SerialNumber, data.OrderNumber,data.ExistOrderNumber,
				   data.Outbounddate, data.TargetLocation, data.SentBy,data.ReceiverName);
				return Ok();
			}
			catch (Exception ex)
			{
				// Log the exception or handle it as needed
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}
		[HttpPost("AddReturnData")]
		public async Task<ActionResult> AddReturnData([FromBody] AddReturnDataList data)
		{
			try
			{
				// Fetch the status of the serial number
				var status = await _context
		.Database
		.SqlQueryRaw<string>("SELECT status FROM [dbo].[sm_Inbound_StockCII] WHERE SerialNumber = @p0 AND MaterialNumber = @p1",
			data.SerialNumber, data.MaterialNumber)
		.ToListAsync();

				// Validate if the status exists and is "Delivered"
				if (string.IsNullOrEmpty(status[0]))
				{
					return NotFound("Serial number or material number not found.");
				}

				if (!string.Equals(status[0], "Outward", StringComparison.OrdinalIgnoreCase))
				{
					return BadRequest("The serial number status should be 'Outward' before returning.");
				}

				// Execute the stored procedure
				await _context.Database.ExecuteSqlRawAsync(
					"EXEC AddReturnStockCII @p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10",
					data.DeliveryNumber, data.MaterialNumber, data.MaterialDescription, data.SerialNumber,
					data.OrderNumber, data.LocationReturnedFrom, data.Returneddate, data.ReturnedBy,
					data.RackLocation, data.ReturnType, data.Returns);

				// Update the stock status
				await _context.Database.ExecuteSqlRawAsync(
					"UPDATE sm_Inbound_StockCII SET Status = @p0 WHERE SerialNumber = @p1 AND MaterialNumber = @p2",
					data.ReturnType, data.SerialNumber, data.MaterialNumber);

				return Ok("Return data added successfully.");
			}
			catch (Exception ex)
			{
				// Log the exception (use a logging framework like Serilog, NLog, etc.)
				Console.WriteLine($"Error: {ex.Message}");

				return StatusCode(500, "An error occurred while processing your request.");
			}
		}

		[HttpPost("UpdateReturnData")]
		public async Task<ActionResult> UpdateReturnData([FromBody] UpdateReturnDataList data)
		{
			try
			{
				await _context.Database.ExecuteSqlRawAsync(@"exec UpdateReturndata @p0, @p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9",
				   data.MaterialNumber, data.SerialNumber, data.OrderNumber,
				   data.LocationReturnedFrom, data.ReturnedDate, data.RackLocation, data.ReturnType, data.ReturnedBy, data.Returns, data.ExistOrderNumber);
				return Ok();
			}


			catch (Exception ex)
			{
				// Log the exception or handle it as needed
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}

		// GET: api/SmOutboundStockCiis/5
		[HttpGet("{id}")]
        public async Task<ActionResult<SmOutboundStockCii>> GetSmOutboundStockCii(string id)
        {
            var smOutboundStockCii = await _context.SmOutboundStockCiis.FindAsync(id);

            if (smOutboundStockCii == null)
            {
                return NotFound();
            }

            return smOutboundStockCii;
        }

        // PUT: api/SmOutboundStockCiis/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSmOutboundStockCii(string id, SmOutboundStockCii smOutboundStockCii)
        {
            if (id != smOutboundStockCii.DeliveryNumber)
            {
                return BadRequest();
            }

            _context.Entry(smOutboundStockCii).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SmOutboundStockCiiExists(id))
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

        // POST: api/SmOutboundStockCiis
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<SmOutboundStockCii>> PostSmOutboundStockCii(SmOutboundStockCii smOutboundStockCii)
        {
            _context.SmOutboundStockCiis.Add(smOutboundStockCii);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (SmOutboundStockCiiExists(smOutboundStockCii.DeliveryNumber))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetSmOutboundStockCii", new { id = smOutboundStockCii.DeliveryNumber }, smOutboundStockCii);
        }

        // DELETE: api/SmOutboundStockCiis/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSmOutboundStockCii(string id)
        {
            var smOutboundStockCii = await _context.SmOutboundStockCiis.FindAsync(id);
            if (smOutboundStockCii == null)
            {
                return NotFound();
            }

            _context.SmOutboundStockCiis.Remove(smOutboundStockCii);
            await _context.SaveChangesAsync();

            return Ok();
        }

        private bool SmOutboundStockCiiExists(string id)
        {
            return _context.SmOutboundStockCiis.Any(e => e.DeliveryNumber == id);
        }
    }
}
