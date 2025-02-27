using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagementWebApi.Models;
using StockManagementWebApi.Models.LoginModel;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace StockManagementWebApi.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class LoginController : ControllerBase
	{
		private readonly MydbContext _context;
		private readonly IWebHostEnvironment _environment;
		private readonly IConfiguration _configuration;

		public LoginController(IWebHostEnvironment environment, IConfiguration configuration, MydbContext context)
		{
			_environment = environment;
			_configuration = configuration;
			_context = context;
		}
		[HttpPost("Login")]
		public async Task<IActionResult> Login([FromBody] Login data)
		{
			try
			{
				var customers = _context.UserLists.FromSqlRaw(@"exec Sp_Login @p0,@p1",data.Email,data.Password).ToList();
				
				if (customers.Count == 0)
				{
					return StatusCode(500, "The User Email or Password is Invalid!!!");
				}
				if (customers[0].IsActive==false)
				{
					return StatusCode(500, "The User was InActive.. Please Contact Admin");
				}

				return Ok(customers);
			}
			catch (Exception ex)
			{
				return StatusCode(500, "An error occurred while processing your request.");
			}
		}
	}
}
