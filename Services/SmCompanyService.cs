using Microsoft.EntityFrameworkCore;
using StockManagementWebApi.Common.Exceptions;
using StockManagementWebApi.Models;

namespace StockManagementWebApi.Services
{
	public interface ISmCompanyService
	{
		Task<IReadOnlyList<SmCompany>> GetAllAsync(CancellationToken cancellationToken = default);

		Task<SmCompany> GetByIdAsync(string id, CancellationToken cancellationToken = default);

		Task UpdateAsync(string id, SmCompany company, CancellationToken cancellationToken = default);

		Task<SmCompany> CreateAsync(SmCompany company, CancellationToken cancellationToken = default);

		Task DeleteAsync(string id, CancellationToken cancellationToken = default);
	}

	/// <summary>Entity-Framework backed CRUD over sm_Companies.</summary>
	public class SmCompanyService : ISmCompanyService
	{
		private readonly MydbContext _context;
		private readonly ILogger<SmCompanyService> _logger;

		public SmCompanyService(MydbContext context, ILogger<SmCompanyService> logger)
		{
			_context = context;
			_logger = logger;
		}

		public async Task<IReadOnlyList<SmCompany>> GetAllAsync(CancellationToken cancellationToken = default)
			=> await _context.SmCompanies.ToListAsync(cancellationToken);

		public async Task<SmCompany> GetByIdAsync(string id, CancellationToken cancellationToken = default)
		{
			var company = await _context.SmCompanies.FindAsync(new object?[] { id }, cancellationToken);

			return company ?? throw NotFoundException.For("Company", id);
		}

		public async Task UpdateAsync(string id, SmCompany company, CancellationToken cancellationToken = default)
		{
			if (company == null)
			{
				throw new BadRequestException("Request data is null.");
			}

			if (id != company.PkCompanyCode)
			{
				throw new ValidationException("The company code in the URL does not match the company code in the body.");
			}

			_context.Entry(company).State = EntityState.Modified;

			try
			{
				await _context.SaveChangesAsync(cancellationToken);
			}
			catch (DbUpdateConcurrencyException exception)
			{
				if (!await ExistsAsync(id, cancellationToken))
				{
					throw new NotFoundException($"Company '{id}' was not found.", innerException: exception);
				}

				// The row exists, so this is a genuine concurrent edit - let the middleware
				// translate it into a 409.
				throw;
			}

			_logger.LogInformation("Company {CompanyId} updated.", id);
		}

		public async Task<SmCompany> CreateAsync(SmCompany company, CancellationToken cancellationToken = default)
		{
			if (company == null)
			{
				throw new BadRequestException("Request data is null.");
			}

			_context.SmCompanies.Add(company);

			try
			{
				await _context.SaveChangesAsync(cancellationToken);
			}
			catch (DbUpdateException exception)
			{
				if (await ExistsAsync(company.PkCompanyCode, cancellationToken))
				{
					throw new ConflictException($"Company '{company.PkCompanyCode}' already exists.", innerException: exception);
				}

				throw;
			}

			_logger.LogInformation("Company {CompanyId} created.", company.PkCompanyCode);
			return company;
		}

		public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
		{
			var company = await _context.SmCompanies.FindAsync(new object?[] { id }, cancellationToken)
				?? throw NotFoundException.For("Company", id);

			_context.SmCompanies.Remove(company);
			await _context.SaveChangesAsync(cancellationToken);

			_logger.LogInformation("Company {CompanyId} deleted.", id);
		}

		private Task<bool> ExistsAsync(string id, CancellationToken cancellationToken)
			=> _context.SmCompanies.AsNoTracking().AnyAsync(e => e.PkCompanyCode == id, cancellationToken);
	}
}
