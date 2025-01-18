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
		[HttpGet]
		public async Task<ActionResult> GetSmInboundNonStockCiis()
		{
			var customers = _context.NonStockCIILists.FromSqlRaw(@"exec Non_StockCIIList ").ToList();
			return Ok(customers);
			
		}

		[HttpGet("GetInwardNonStockCiis/{MaterialNumber}")]
		public async Task<ActionResult> GetInwardNonStockCiis(string MaterialNumber)
		{
			var customers = _context.SmInboundStockNonCiis.FromSqlRaw(@"exec sp_getInboundStock_NonCII @p0", MaterialNumber).ToList();
			return Ok(customers);

		}


		[HttpPost("AddNonStockInbounddata")]
		public async Task<IActionResult> AddNonStockInbounddata([FromBody] AddNonStockInward data)
		{
			try
			{
				await _context.Database.ExecuteSqlRawAsync(@"exec Sp_AddInboundStock_NonCII @p0, @p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8", data.DeliveryNumber, data.OrderNumber, data.MaterialNumber,  
					data.MaterialDescription, data.Inwarddate, data.InwardFrom, data.ReceivedBy,data.RacKLocation, data.QuantityReceived);
				return Ok();
			}
			catch (Exception ex)
			{
				// Log the exception or handle it as needed
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}

		[HttpPost("DeleteNonStockInbounddata/{MaterialNumber}/{DeliveryNumber}/{OrderNumber}")]
		public async Task<IActionResult> DeleteNonStockInbounddata(string MaterialNumber,string DeliveryNumber,string OrderNumber)
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
					data.ExistDeliveryNumber,data.ExistOrderNumber, data.Inwarddate, data.InwardFrom, data.ReceivedBy, data.RacKLocation, data.QuantityReceived);
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
				await _context.Database.ExecuteSqlRawAsync(@"exec Sp_AddOutboundStock_NonCII @p0, @p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9", data.DeliveryNumber, data.OrderNumber, data.MaterialNumber,
					data.MaterialDescription, data.OutboundDate, data.ReceiverName, data.TargetLocation, data.DeliveredQuantity, data.SentBy,data.DeliveryNumber_inbound);
				return Ok();
			}
			catch (Exception ex)
			{
				// Log the exception or handle it as needed
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}

        
		[HttpGet("DeliveredDataList")]
		public async Task<ActionResult> GetDeliveredDataList()
		{
			var customers = _context.SmOutboundStockNonCiis.FromSqlRaw(@"select * from sm_OutboundStock_NonCII").ToList();
			return Ok(customers);

		}

		[HttpPost("DeleteNonStockDeliverdata/{MaterialNumber}/{DeliveryNumber}/{OrderNumber}")]
		public async Task<IActionResult> DeleteNonStockDeliverdata(string MaterialNumber, string DeliveryNumber, string OrderNumber)
		{
			try
			{
				await _context.Database.ExecuteSqlRawAsync(@"exec Sp_DeleteNonStockDeliverCII @p0, @p1,@p2", MaterialNumber, DeliveryNumber, OrderNumber);
				return Ok();
			}
			catch (Exception ex)
			{
				// Log the exception or handle it as needed
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}

		[HttpPost("UpdateNonStockDeliverdata")]
		public async Task<IActionResult> UpdateNonStockDeliverdata([FromBody] UpdateNonStockDeliverData data)
		{
			try
			{
				await _context.Database.ExecuteSqlRawAsync(@"exec Sp_UpdateOutboundStock_NonCII @p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10", data.DeliveryNumber, data.OrderNumber, 
					data.ExistDeliveryNumber,data.ExistOrderNumber, data.MaterialNumber,
					 data.OutboundDate, data.ReceiverName, data.TargetLocation, data.DeliveredQuantity, data.SentBy);
				return Ok();
			}
			catch (Exception ex)
			{
				// Log the exception or handle it as needed
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}



		[HttpPost("AddNonStockReturndata")]
		public async Task<IActionResult> AddNonStockReturndata([FromBody] AddNonStockReturnData data)
		{
			try
			{
				await _context.Database.ExecuteSqlRawAsync(@"exec sp_AddReturnNonStockData @p0, @p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8",data.OrderNumber, data.MaterialNumber,
					data.ReturnLocation, data.Returndate, data.ReturnQuantity, data.ReceivedBy, data.RackLocation, data.ReturnType, data.Reason);
				return Ok();
			}
			catch (Exception ex)
			{
				// Log the exception or handle it as needed
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}

		[HttpPost("UpdateNonStockReturndata")]
		public async Task<IActionResult> UpdateNonStockReturndata([FromBody] UpdateNonStockRetundata data)
		{
			try
			{
				await _context.Database.ExecuteSqlRawAsync(@"exec sp_UpdateReturnNonStockData @p0, @p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9", data.OrderNumber,data.ExistOrderNumber, data.MaterialNumber,
					data.ReturnLocation, data.Returndate, data.ReturnQuantity, data.ReceivedBy, data.RackLocation, data.ReturnType, data.Reason);
				return Ok();
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
