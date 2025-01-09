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

		[HttpPost("{MaterialNumber}/{SerialNumber}")]
		public async Task<ActionResult<StockCiiList>> outboundstockList(string MaterialNumber, string SerialNumber)
		{
			try
			{
				var Inbounddata = _context.OutboundDataLists.FromSqlRaw(@"exec sp_outboundstockList @p0, @p1", MaterialNumber, SerialNumber).ToList();
				var Deliverydata = _context.SmReturnStockCiis.FromSqlRaw(@"exec deliverystockCII @p0", SerialNumber).ToList();

				return Ok(
                    new
                    {
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
			try
			{
				await _context.Database.ExecuteSqlRawAsync(@"exec AddInboundStockCII @p0, @p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8",
                   data.DeliveryNumber,data.MaterialNumber,data.SerialNumber,data.MaterialDescription,data.OrderNumber,
                   data.OutBounddate, data.TargetLocation, data.SentBy, data.Fk_Inbound_StockCII_DeliveryNumber);
				return Ok();
	         }
			catch (Exception ex)
			{
				// Log the exception or handle it as needed
				return StatusCode(500, "An error occurred while processing your request.");
			}
			
		}

		[HttpPost("UpdatedeliveryData")]
		public async Task<ActionResult> UpdatedeliveryData([FromBody] UpdatedeliveryDataList data)
		{
             
			try
			{
				await _context.Database.ExecuteSqlRawAsync(@"exec updatedeliverydata @p0, @p1,@p2,@p3,@p4,@p5",
				  data.MaterialNumber, data.SerialNumber, data.OrderNumber,
				   data.Outbounddate, data.TargetLocation, data.SentBy);
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
				await _context.Database.ExecuteSqlRawAsync(@"exec AddReturnStockCII @p0, @p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10",
				   data.DeliveryNumber, data.MaterialNumber, data.MaterialDescription, data.SerialNumber,  data.OrderNumber,
				   data.LocationReturnedFrom, data.Returneddate, data.ReturnedBy, data.RackLocation,data.ReturnType,data.Returns);
				return Ok();
			}

			catch (Exception ex)
			{
				// Log the exception or handle it as needed
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}
		[HttpPost("UpdateReturnData")]
		public async Task<ActionResult> UpdateReturnData([FromBody] UpdateReturnDataList data)
		{
			try
			{
				await _context.Database.ExecuteSqlRawAsync(@"exec UpdateReturndata @p0, @p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8",
				   data.MaterialNumber, data.SerialNumber, data.OrderNumber,
				   data.LocationReturnedFrom, data.ReturnedDate, data.RackLocation, data.ReturnType, data.ReturnedBy, data.Returns);
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
