using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagementWebApi.Models;
using StockManagementWebApi.Models.UserManagement;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace StockManagementWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserManagementController : ControllerBase
    {
        private readonly MydbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public UserManagementController(IWebHostEnvironment environment, IConfiguration configuration, MydbContext context)
        {
            _environment = environment;
            _configuration = configuration;
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult> GetCompanyList()
        {
            var customers = _context.CompanyLists.FromSqlRaw(@"exec CompanyList ").ToList();
            return Ok(customers);

        }

        [HttpPost("AddCompanyUserManagement")]
        public async Task<IActionResult> AddCompanyUserManagement([FromBody] AddCompany data)
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync(@"exec AddCompany @p0,@p1,@p2", data.CompanyId,data.CompanyName,data.DomainName);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }
		[HttpPost("UpdateCompanyUserManagement")]
		public async Task<IActionResult> UpdateCompanyUserManagement([FromBody] UpdateCompany data)
		{
			try
			{
				await _context.Database.ExecuteSqlRawAsync(@"exec UpdateCompany @p0,@p1,@p2,@p3", data.CompanyId,data.ExistCompanyId, data.CompanyName, data.DomainName);
				return Ok();
			}
			catch (Exception ex)
			{
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}
		[HttpPost("DeleteCompanyUserManagement/{CompanyId}")]
		public async Task<IActionResult> DeleteCompanyUserManagement(string CompanyId)
		{
			try
			{
				await _context.Database.ExecuteSqlRawAsync(
					"UPDATE sm_Companies SET CompanyStatus = 0 WHERE Pk_CompanyCode = {0}", CompanyId);

				return Ok("Company status updated successfully.");
			}
			catch (Exception ex)
			{
				// Log the exception details (if logging is configured)
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}



	}
}
