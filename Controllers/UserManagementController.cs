using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagementWebApi.Models;

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



    }
}
