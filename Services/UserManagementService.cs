using Microsoft.EntityFrameworkCore;
using StockManagementWebApi.Common.Exceptions;
using StockManagementWebApi.Models;
using StockManagementWebApi.Models.UserManagement;

namespace StockManagementWebApi.Services
{
	public interface IUserManagementService
	{
		Task<IReadOnlyList<CompanyList>> GetCompanyListAsync(string userName, CancellationToken cancellationToken = default);

		Task AddCompanyAsync(AddCompany data, CancellationToken cancellationToken = default);

		Task UpdateCompanyAsync(UpdateCompany data, CancellationToken cancellationToken = default);

		Task DeactivateCompanyAsync(string companyId, CancellationToken cancellationToken = default);

		Task<IReadOnlyList<TenetList>> GetTenantListAsync(string companyCode, CancellationToken cancellationToken = default);

		Task AddTenantAsync(AddTenet data, CancellationToken cancellationToken = default);

		Task UpdateTenantAsync(UpdateTenet data, CancellationToken cancellationToken = default);

		Task DeactivateTenantAsync(string tenantId, string companyId, CancellationToken cancellationToken = default);

		Task AddUserAsync(AddUser data, CancellationToken cancellationToken = default);

		Task<IReadOnlyList<UserList>> GetUserListAsync(string tenantCode, CancellationToken cancellationToken = default);

		Task UpdateUserAsync(UpdateUser data, CancellationToken cancellationToken = default);

		Task DeactivateUserAsync(int userId, CancellationToken cancellationToken = default);
	}

	/// <summary>Company, tenant and user administration. All work goes through stored procedures.</summary>
	public class UserManagementService : IUserManagementService
	{
		private readonly MydbContext _context;
		private readonly ILogger<UserManagementService> _logger;

		public UserManagementService(MydbContext context, ILogger<UserManagementService> logger)
		{
			_context = context;
			_logger = logger;
		}

		// ------------------------------------------------------------------ Companies

		public async Task<IReadOnlyList<CompanyList>> GetCompanyListAsync(string userName, CancellationToken cancellationToken = default)
		{
			Require(userName, nameof(userName));

			return await _context.CompanyLists
				.FromSqlRaw(@"exec CompanyList @p0", userName)
				.ToListAsync(cancellationToken);
		}

		public async Task AddCompanyAsync(AddCompany data, CancellationToken cancellationToken = default)
		{
			RequireBody(data);

			await _context.Database.ExecuteSqlRawAsync(
				@"exec AddCompany @p0,@p1,@p2",
				new object?[] { data.CompanyId, data.CompanyName, data.DomainName }!,
				cancellationToken);

			_logger.LogInformation("Company {CompanyId} created.", data.CompanyId);
		}

		public async Task UpdateCompanyAsync(UpdateCompany data, CancellationToken cancellationToken = default)
		{
			RequireBody(data);

			await _context.Database.ExecuteSqlRawAsync(
				@"exec UpdateCompany @p0,@p1,@p2,@p3",
				new object?[] { data.CompanyId, data.ExistCompanyId, data.CompanyName, data.DomainName }!,
				cancellationToken);

			_logger.LogInformation("Company {CompanyId} updated.", data.CompanyId);
		}

		public async Task DeactivateCompanyAsync(string companyId, CancellationToken cancellationToken = default)
		{
			Require(companyId, nameof(companyId));

			await _context.Database.ExecuteSqlRawAsync(
				"UPDATE sm_Companies SET CompanyStatus = 0 WHERE Pk_CompanyCode = {0}",
				new object[] { companyId },
				cancellationToken);

			_logger.LogInformation("Company {CompanyId} marked inactive.", companyId);
		}

		// ------------------------------------------------------------------ Tenants

		public async Task<IReadOnlyList<TenetList>> GetTenantListAsync(string companyCode, CancellationToken cancellationToken = default)
		{
			Require(companyCode, nameof(companyCode));

			return await _context.TenetLists
				.FromSqlRaw(@"exec TenentList @p0", companyCode)
				.ToListAsync(cancellationToken);
		}

		public async Task AddTenantAsync(AddTenet data, CancellationToken cancellationToken = default)
		{
			RequireBody(data);

			await _context.Database.ExecuteSqlRawAsync(
				@"exec addtenent @p0,@p1,@p2,@p3",
				new object?[] { data.TenentCode, data.TenentLocation, data.TenentName, data.CompanyCode }!,
				cancellationToken);

			_logger.LogInformation("Tenant {TenantCode} created under company {CompanyCode}.", data.TenentCode, data.CompanyCode);
		}

		public async Task UpdateTenantAsync(UpdateTenet data, CancellationToken cancellationToken = default)
		{
			RequireBody(data);

			await _context.Database.ExecuteSqlRawAsync(
				@"exec updatetenent @p0,@p1,@p2,@p3,@p4",
				new object?[] { data.TenentCode, data.ExistTenentCode, data.TenentLocation, data.TenentName, data.CompanyCode }!,
				cancellationToken);

			_logger.LogInformation("Tenant {TenantCode} updated.", data.TenentCode);
		}

		public async Task DeactivateTenantAsync(string tenantId, string companyId, CancellationToken cancellationToken = default)
		{
			Require(tenantId, nameof(tenantId));
			Require(companyId, nameof(companyId));

			await _context.Database.ExecuteSqlRawAsync(
				"UPDATE sm_Tenents SET TenentStatus = 0 WHERE Pk_TenentCode = {0} and Fk_CompanyCode={1}",
				new object[] { tenantId, companyId },
				cancellationToken);

			_logger.LogInformation("Tenant {TenantId} of company {CompanyId} marked inactive.", tenantId, companyId);
		}

		// ------------------------------------------------------------------ Users

		public async Task AddUserAsync(AddUser data, CancellationToken cancellationToken = default)
		{
			RequireBody(data);

			var existing = await _context.Database
				.SqlQueryRaw<string>("select LoginId from sm_Users where LoginId={0} ", data.Email)
				.ToListAsync(cancellationToken);

			if (existing.Count > 0)
			{
				throw new ConflictException("The User Email Already Exist!!!");
			}

			await _context.Database.ExecuteSqlRawAsync(
				@"exec AddUser @p0,@p1,@p2,@p3,@p4,@p5,@p6",
				new object?[]
				{
					data.UserCode, data.UserName, data.Email, data.UserType,
					data.AccessLevel, data.Password, data.TenentCode
				}!,
				cancellationToken);

			_logger.LogInformation("User {Email} created for tenant {TenantCode}.", data.Email, data.TenentCode);
		}

		public async Task<IReadOnlyList<UserList>> GetUserListAsync(string tenantCode, CancellationToken cancellationToken = default)
		{
			Require(tenantCode, nameof(tenantCode));

			return await _context.UserLists
				.FromSqlRaw(@"exec UserList @p0", tenantCode)
				.ToListAsync(cancellationToken);
		}

		public async Task UpdateUserAsync(UpdateUser data, CancellationToken cancellationToken = default)
		{
			RequireBody(data);

			await _context.Database.ExecuteSqlRawAsync(
				@"exec UpdateUser @p0,@p1,@p2,@p3,@p4",
				new object?[] { data.UserCode, data.UserName, data.UserType, data.AccessLevel, data.UserStatus }!,
				cancellationToken);

			_logger.LogInformation("User {UserCode} updated.", data.UserCode);
		}

		public async Task DeactivateUserAsync(int userId, CancellationToken cancellationToken = default)
		{
			await _context.Database.ExecuteSqlRawAsync(
				"UPDATE sm_Users SET IsActive = 0 WHERE Pk_UserCode = {0}",
				new object[] { userId },
				cancellationToken);

			_logger.LogInformation("User {UserId} marked inactive.", userId);
		}

		// ------------------------------------------------------------------ Helpers

		private static void Require(string value, string parameterName)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				throw new ValidationException($"{parameterName} is required.");
			}
		}

		private static void RequireBody<T>(T? data) where T : class
		{
			if (data == null)
			{
				throw new BadRequestException("Request data is null.");
			}
		}
	}
}
