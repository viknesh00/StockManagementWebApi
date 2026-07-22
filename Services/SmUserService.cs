using Microsoft.EntityFrameworkCore;
using StockManagementWebApi.Common.Exceptions;
using StockManagementWebApi.Models;

namespace StockManagementWebApi.Services
{
	public interface ISmUserService
	{
		Task<IReadOnlyList<SmUser>> GetAllAsync(CancellationToken cancellationToken = default);

		Task<SmUser> GetByIdAsync(int id, CancellationToken cancellationToken = default);

		Task UpdateAsync(int id, SmUser user, CancellationToken cancellationToken = default);

		Task<SmUser> CreateAsync(SmUser user, CancellationToken cancellationToken = default);

		Task DeleteAsync(int id, CancellationToken cancellationToken = default);
	}

	/// <summary>Entity-Framework backed CRUD over sm_Users.</summary>
	public class SmUserService : ISmUserService
	{
		private readonly MydbContext _context;
		private readonly ILogger<SmUserService> _logger;

		public SmUserService(MydbContext context, ILogger<SmUserService> logger)
		{
			_context = context;
			_logger = logger;
		}

		public async Task<IReadOnlyList<SmUser>> GetAllAsync(CancellationToken cancellationToken = default)
			=> await _context.SmUsers.ToListAsync(cancellationToken);

		public async Task<SmUser> GetByIdAsync(int id, CancellationToken cancellationToken = default)
		{
			var user = await _context.SmUsers.FindAsync(new object?[] { id }, cancellationToken);

			return user ?? throw NotFoundException.For("User", id);
		}

		public async Task UpdateAsync(int id, SmUser user, CancellationToken cancellationToken = default)
		{
			if (user == null)
			{
				throw new BadRequestException("Request data is null.");
			}

			if (id != user.PkUserCode)
			{
				throw new ValidationException("The user code in the URL does not match the user code in the body.");
			}

			_context.Entry(user).State = EntityState.Modified;

			try
			{
				await _context.SaveChangesAsync(cancellationToken);
			}
			catch (DbUpdateConcurrencyException exception)
			{
				if (!await ExistsAsync(id, cancellationToken))
				{
					throw new NotFoundException($"User '{id}' was not found.", innerException: exception);
				}

				throw;
			}

			_logger.LogInformation("User {UserId} updated.", id);
		}

		public async Task<SmUser> CreateAsync(SmUser user, CancellationToken cancellationToken = default)
		{
			if (user == null)
			{
				throw new BadRequestException("Request data is null.");
			}

			_context.SmUsers.Add(user);

			try
			{
				await _context.SaveChangesAsync(cancellationToken);
			}
			catch (DbUpdateException exception)
			{
				if (await ExistsAsync(user.PkUserCode, cancellationToken))
				{
					throw new ConflictException($"User '{user.PkUserCode}' already exists.", innerException: exception);
				}

				throw;
			}

			_logger.LogInformation("User {UserId} created.", user.PkUserCode);
			return user;
		}

		public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
		{
			var user = await _context.SmUsers.FindAsync(new object?[] { id }, cancellationToken)
				?? throw NotFoundException.For("User", id);

			_context.SmUsers.Remove(user);
			await _context.SaveChangesAsync(cancellationToken);

			_logger.LogInformation("User {UserId} deleted.", id);
		}

		private Task<bool> ExistsAsync(int id, CancellationToken cancellationToken)
			=> _context.SmUsers.AsNoTracking().AnyAsync(e => e.PkUserCode == id, cancellationToken);
	}
}
