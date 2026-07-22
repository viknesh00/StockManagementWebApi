using Microsoft.AspNetCore.Mvc;
using StockManagementWebApi.Common.Controllers;
using StockManagementWebApi.Models;
using StockManagementWebApi.Services;

namespace StockManagementWebApi.Controllers
{
	[Route("api/[controller]")]
	public class SmCompaniesController : BaseApiController
	{
		private readonly ISmCompanyService _companyService;

		public SmCompaniesController(ISmCompanyService companyService)
		{
			_companyService = companyService;
		}

		// GET: api/SmCompanies
		[HttpGet]
		public async Task<IActionResult> GetSmCompanies(CancellationToken cancellationToken)
		{
			var companies = await _companyService.GetAllAsync(cancellationToken);

			return Success(companies, "Companies retrieved successfully.");
		}

		// GET: api/SmCompanies/5
		[HttpGet("{id}")]
		public async Task<IActionResult> GetSmCompany(string id, CancellationToken cancellationToken)
		{
			var company = await _companyService.GetByIdAsync(id, cancellationToken);

			return Success(company, "Company retrieved successfully.");
		}

		// PUT: api/SmCompanies/5
		[HttpPut("{id}")]
		public async Task<IActionResult> PutSmCompany(string id, SmCompany smCompany, CancellationToken cancellationToken)
		{
			await _companyService.UpdateAsync(id, smCompany, cancellationToken);

			return Updated(message: "Company updated successfully.");
		}

		// POST: api/SmCompanies
		[HttpPost]
		public async Task<IActionResult> PostSmCompany(SmCompany smCompany, CancellationToken cancellationToken)
		{
			var created = await _companyService.CreateAsync(smCompany, cancellationToken);

			return Created(created, "Company created successfully.", Url.Action(nameof(GetSmCompany), new { id = created.PkCompanyCode }));
		}

		// DELETE: api/SmCompanies/5
		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteSmCompany(string id, CancellationToken cancellationToken)
		{
			await _companyService.DeleteAsync(id, cancellationToken);

			return Deleted(message: "Company deleted successfully.");
		}
	}
}
