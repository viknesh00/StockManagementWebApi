using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using OfficeOpenXml;
using StockManagementWebApi.Models;

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
        [HttpGet]
        public async Task<ActionResult> GetSmInboundStockCiis()
        {
			var customers = _context.StockCiiLists.FromSqlRaw(@"exec StockCIIList ").ToList();
			return Ok(customers);
			//return await _context.SmInboundStockCiis.ToListAsync();
        }


		[HttpPost("import")]
		public async Task<IActionResult> ImportStockData( AddStockInward data)
		{
			if (data.file == null || data.file.Length == 0)
				return BadRequest("No file uploaded.");

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
						var stock = new Dictionary<string, object>
					{
							{ "SerialNumber", worksheet.Cells[row, 1].Text },
							{ "Quantity", int.TryParse(worksheet.Cells[row, 2].Text, out int qty) ? qty : 0 },
							{ "Status", worksheet.Cells[row, 3].Text },
							{ "DeliveryNumber", worksheet.Cells[row, 4].Text },
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

				if (inboundStocks.Count == 0)
					return BadRequest("The Excel file contains no data.");

				// Insert data into the database
				var connectionString = _configuration.GetConnectionString("MyDBConnection");
				using (var connection = new SqlConnection(connectionString))
				{
					await connection.OpenAsync();

					foreach (var stock in inboundStocks)
					{
						var query = @"
                        INSERT INTO sm_Inbound_StockCII (DeliveryNumber, OrderNumber, MaterialNumber, MaterialDescription,SerialNumber,Quantity,InwardDate,SourceLocation,ReceivedBy,Status,RackLocation,Fk_UserCode)
                        VALUES (@DeliveryNumber, @OrderNumber, @MaterialNumber, @MaterialDescription,@SerialNumber,@Quantity,@InwardDate,@SourceLocation,@ReceivedBy,@Status,@RackLocation,1);";

						using (var command = new SqlCommand(query, connection))
						{
							//command.Parameters.AddWithValue("@DeliveryNumber", stock["DeliveryNumber"] ?? DBNull.Value);
							//command.Parameters.AddWithValue("@OrderNumber", stock["OrderNumber"] ?? DBNull.Value);
							//command.Parameters.AddWithValue("@MaterialNumber", stock["MaterialNumber"] ?? DBNull.Value);
							//command.Parameters.AddWithValue("@MaterialDescription", stock["MaterialDescription"] ?? DBNull.Value);
							//command.Parameters.AddWithValue("@SerialNumber", stock["SerialNumber"] ?? DBNull.Value);
							//command.Parameters.AddWithValue("@Quantity", stock["Quantity"] ?? DBNull.Value);
							//command.Parameters.AddWithValue("@InwardDate", stock["InwardDate"] ?? DBNull.Value);
							//command.Parameters.AddWithValue("@SourceLocation", stock["SourceLocation"] ?? DBNull.Value);
							//command.Parameters.AddWithValue("@ReceivedBy", stock["ReceivedBy"] ?? DBNull.Value);
							//command.Parameters.AddWithValue("@Status", stock["Status"] ?? DBNull.Value);
							//command.Parameters.AddWithValue("@RackLocation", stock["RackLocation"] ?? DBNull.Value);
							command.Parameters.AddWithValue("@DeliveryNumber", stock["DeliveryNumber"] ?? DBNull.Value);
							command.Parameters.AddWithValue("@OrderNumber", data.OrderNumber );
							command.Parameters.AddWithValue("@MaterialNumber", data.MaterialNumber);
							command.Parameters.AddWithValue("@MaterialDescription",data.MaterialDescription);
							command.Parameters.AddWithValue("@SerialNumber", stock["SerialNumber"] ?? DBNull.Value);
							command.Parameters.AddWithValue("@Quantity", stock["Quantity"] ?? DBNull.Value);
							command.Parameters.AddWithValue("@InwardDate", data.Inwarddate);
							command.Parameters.AddWithValue("@SourceLocation", data.InwardFrom);
							command.Parameters.AddWithValue("@ReceivedBy", data.ReceivedBy);
							command.Parameters.AddWithValue("@Status", stock["Status"] ?? DBNull.Value);
							command.Parameters.AddWithValue("@RackLocation", data.RacKLocation);


							await command.ExecuteNonQueryAsync();
						}
					}
				}

				return Ok("Data imported successfully.");
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

		[HttpPost("UpdateInbounddata")]
		public async Task<IActionResult> UpdateInbounddata([FromBody]UpdateInboundData data)
		{
			try
			{
				await _context.Database.ExecuteSqlRawAsync(@"exec updateInboundStockCII @p0, @p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8", data.MaterialNumber, data.SerialNumber,data.ExistSerialNumber,
					data.RackLocation,data.DeliveryNumber,data.OrderNumber,data.InwardDate,data.InwardFrom,data.ReceivedBy);
				return Ok();
			}
			catch (Exception ex)
			{
				// Log the exception or handle it as needed
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}



		// GET: api/SmInboundStockCiis/5
		[HttpGet("{MaterialNumber}")]
        public async Task<ActionResult<SmInboundStockCii>> GetSmInboundStockCii(string MaterialNumber)
        {
			var customers = _context.SmInboundStockCiis.FromSqlRaw(@"exec StockCIIListBySerialNumber @p0", MaterialNumber).ToList();
			return Ok(customers);
		}


		[HttpPost("Material/{MaterialNumber}/{MaterialDescription}")]
		public async Task<IActionResult> AddMaterialNumber(string MaterialNumber, string MaterialDescription)
		{
			try
			{
				await _context.Database.ExecuteSqlRawAsync(@"exec AddMaterialNumber @p0, @p1", MaterialNumber, MaterialDescription);
				return Ok(); 
			}
			catch (Exception ex)
			{
				// Log the exception or handle it as needed
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}
        [HttpPost("update/{ExistMaterialNumber}/{MaterialNumber}/{MaterialDescription}")]
        public async Task<IActionResult> UpdateMaterialNumber(string ExistMaterialNumber, string MaterialNumber, string MaterialDescription)
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync(@"exec updatematerialNumber @p0, @p1, @p2", ExistMaterialNumber, MaterialNumber, MaterialDescription);
                return Ok();
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

		[HttpPost("{MaterialNumber}/{IsActive}")]
		public async Task<ActionResult<StockCiiList>> deleteMaterialNumber(string MaterialNumber, bool IsActive)
		{
			try
			{

				await _context.Database.ExecuteSqlRawAsync(@"exec deletematerialnumber @p0, @p1", MaterialNumber, IsActive);
				return Ok();
			}
			catch (Exception ex)
			{
				// Log the exception or handle it as needed
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}
		[HttpPost("serial/{MaterialNumber}/{IsActive}")]
        public async Task<ActionResult<SmInboundStockCii>> deleteSerialNumber(string MaterialNumber, bool IsActive)
        {
            try
            {
                var customers = _context.SmInboundStockCiis.FromSqlRaw(@"exec sp_deleteserialNumber @p0, @p1", MaterialNumber, IsActive).ToList();
                return Ok(customers);

            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpPost("{MaterialNumber}/{SerialNumber}/{status}")]
        public async Task<ActionResult<SmInboundStockCii>> updateserialstatus(string MaterialNumber, string SerialNumber, string status)
        {
            try
            {
                var customers = _context.SmInboundStockCiis.FromSqlRaw(@"exec sp_updateserialstatus @p0, @p1, @p2", MaterialNumber, SerialNumber, status).ToList();
                return Ok(customers);

            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
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
