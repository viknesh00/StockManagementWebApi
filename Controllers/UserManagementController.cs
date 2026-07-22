using Microsoft.AspNetCore.Mvc;
using StockManagementWebApi.Common.Controllers;
using StockManagementWebApi.Models;
using StockManagementWebApi.Models.UserManagement;
using StockManagementWebApi.Services;

namespace StockManagementWebApi.Controllers
{
	[Route("api/[controller]")]
	public class UserManagementController : BaseApiController
	{
		private readonly IUserManagementService _userManagementService;

		public UserManagementController(IUserManagementService userManagementService)
		{
			_userManagementService = userManagementService;
		}

		// ------------------------------------------------------------------ Companies

		[HttpGet("GetCompanyList/{UserName}")]
		public async Task<IActionResult> GetCompanyList(string UserName, CancellationToken cancellationToken)
		{
			var companies = await _userManagementService.GetCompanyListAsync(UserName, cancellationToken);

			return Success(companies, "Company list retrieved successfully.");
		}

		[HttpPost("AddCompanyUserManagement")]
		public async Task<IActionResult> AddCompanyUserManagement([FromBody] AddCompany data, CancellationToken cancellationToken)
		{
			await _userManagementService.AddCompanyAsync(data, cancellationToken);

			return Success(message: "Company created successfully.");
		}

		[HttpPost("UpdateCompanyUserManagement")]
		public async Task<IActionResult> UpdateCompanyUserManagement([FromBody] UpdateCompany data, CancellationToken cancellationToken)
		{
			await _userManagementService.UpdateCompanyAsync(data, cancellationToken);

			return Updated(message: "Company updated successfully.");
		}

		[HttpPost("DeleteCompanyUserManagement/{CompanyId}")]
		public async Task<IActionResult> DeleteCompanyUserManagement(string CompanyId, CancellationToken cancellationToken)
		{
			await _userManagementService.DeactivateCompanyAsync(CompanyId, cancellationToken);

			return Deleted(message: "Company status updated successfully.");
		}

		// ------------------------------------------------------------------ Tenants

		[HttpGet("GetTenetList/{CompanyCode}")]
		public async Task<IActionResult> GetTenetList(string CompanyCode, CancellationToken cancellationToken)
		{
			var tenants = await _userManagementService.GetTenantListAsync(CompanyCode, cancellationToken);

			return Success(tenants, "Tenant list retrieved successfully.");
		}

		[HttpPost("AddTenet")]
		public async Task<IActionResult> AddTenet([FromBody] AddTenet data, CancellationToken cancellationToken)
		{
			await _userManagementService.AddTenantAsync(data, cancellationToken);

			return Success(message: "Tenant created successfully.");
		}

		[HttpPost("UpdateTenet")]
		public async Task<IActionResult> UpdateTenet([FromBody] UpdateTenet data, CancellationToken cancellationToken)
		{
			await _userManagementService.UpdateTenantAsync(data, cancellationToken);

			return Updated(message: "Tenant updated successfully.");
		}

		[HttpPost("DeleteTenet/{CompanyId}/{TenetIdId}")]
		public async Task<IActionResult> DeleteTenet(string TenetIdId, string CompanyId, CancellationToken cancellationToken)
		{
			await _userManagementService.DeactivateTenantAsync(TenetIdId, CompanyId, cancellationToken);

			return Deleted(message: "Company Tenent deleted successfully.");
		}

		// ------------------------------------------------------------------ Users

		[HttpPost("AddUser")]
		public async Task<IActionResult> AddUser([FromBody] AddUser data, CancellationToken cancellationToken)
		{
			await _userManagementService.AddUserAsync(data, cancellationToken);

			return Success(message: "User created successfully.");
		}

		[HttpGet("GetUserList/{TenentCode}")]
		public async Task<IActionResult> GetUserList(string TenentCode, CancellationToken cancellationToken)
		{
			var users = await _userManagementService.GetUserListAsync(TenentCode, cancellationToken);

			return Success(users, "User list retrieved successfully.");
		}

		[HttpPost("UpdateUser")]
		public async Task<IActionResult> UpdateUser([FromBody] UpdateUser data, CancellationToken cancellationToken)
		{
			await _userManagementService.UpdateUserAsync(data, cancellationToken);

			return Updated(message: "User updated successfully.");
		}

		[HttpPost("DeleteUser/{UserId}")]
		public async Task<IActionResult> DeleteUser(int UserId, CancellationToken cancellationToken)
		{
			await _userManagementService.DeactivateUserAsync(UserId, cancellationToken);

			return Deleted(message: "User Deleted successfully.");
		}
	}
}
