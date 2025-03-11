using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
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

        [HttpGet("GetCompanyList/{UserName}")]
        public async Task<ActionResult> GetCompanyList(string UserName)
        {
            var customers = _context.CompanyLists.FromSqlRaw(@"exec CompanyList @p0", UserName).ToList();
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

		[HttpGet("GetTenetList/{CompanyCode}")]
		public async Task<ActionResult> GetTenetList( string CompanyCode)
		{
			var customers = _context.TenetLists.FromSqlRaw(@"exec TenentList @p0", CompanyCode).ToList();
			return Ok(customers);

		}

		[HttpPost("AddTenet")]
		public async Task<IActionResult> AddTenet([FromBody] AddTenet data)
		{
			try
			{
				await _context.Database.ExecuteSqlRawAsync(@"exec addtenent @p0,@p1,@p2,@p3", data.TenentCode, data.TenentLocation, data.TenentName, data.CompanyCode);
				return Ok();
			}
			catch (Exception ex)
			{
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}

		[HttpPost("UpdateTenet")]
		public async Task<IActionResult> UpdateTenet([FromBody] UpdateTenet data)
		{
			try
			{
				await _context.Database.ExecuteSqlRawAsync(@"exec updatetenent @p0,@p1,@p2,@p3,@p4", data.TenentCode,data.ExistTenentCode, data.TenentLocation, data.TenentName, data.CompanyCode);
				return Ok();
			}
			catch (Exception ex)
			{
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}


		[HttpPost("DeleteTenet/{CompanyId}/{TenetIdId}")]
		public async Task<IActionResult> DeleteTenet(string TenetIdId, string CompanyId)
		{
			try
			{
				await _context.Database.ExecuteSqlRawAsync(
					"UPDATE sm_Tenents SET TenentStatus = 0 WHERE Pk_TenentCode = {0} and Fk_CompanyCode={1}", TenetIdId, CompanyId);

				return Ok("Company Tenent deleted successfully.");
			}
			catch (Exception ex)
			{
				// Log the exception details (if logging is configured)
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}



		

		//User Management

		[HttpPost("AddUser")]
		public async Task<IActionResult> AddUser([FromBody] AddUser data)
		{
			try
			{
				var customers = _context.Database.SqlQueryRaw<string>("select LoginId from sm_Users where LoginId={0} ", data.Email).ToList();

				if (customers.Count > 0) 
				{
					return StatusCode(500, "The User Email Already Exist!!!");
				}

				await _context.Database.ExecuteSqlRawAsync(@"exec AddUser @p0,@p1,@p2,@p3,@p4,@p5,@p6",
					data.UserCode, data.UserName, data.Email, data.UserType, data.AccessLevel, data.Password,data.TenentCode);
				return Ok();
			}
			catch (Exception ex)
			{
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}

		[HttpGet("GetUserList/{TenentCode}")]
		public async Task<ActionResult> GetUserList( string TenentCode)
		{
			var customers = _context.UserLists.FromSqlRaw(@"exec UserList @p0", TenentCode).ToList();
			return Ok(customers);

		}

		[HttpPost("UpdateUser")]
		public async Task<IActionResult> UpdateUser([FromBody] UpdateUser data)
		{
			try
			{
				await _context.Database.ExecuteSqlRawAsync(@"exec UpdateUser @p0,@p1,@p2,@p3,@p4", data.UserCode, data.UserName, data.UserType, data.AccessLevel,data.UserStatus);
				return Ok();
			}
			catch (Exception ex)
			{
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}


		[HttpPost("DeleteUser/{UserId}")]
		public async Task<IActionResult> DeleteUser(int UserId)
		{
			try
			{
				await _context.Database.ExecuteSqlRawAsync(
					"UPDATE sm_Users SET IsActive = 0 WHERE Pk_UserCode = {0}", UserId);

				return Ok("User Deleted successfully.");
			}
			catch (Exception ex)
			{
				// Log the exception details (if logging is configured)
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}





	}
}
