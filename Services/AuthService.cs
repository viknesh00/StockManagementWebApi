using Microsoft.EntityFrameworkCore;
using StockManagementWebApi.Common.Exceptions;
using StockManagementWebApi.Models;
using StockManagementWebApi.Models.LoginModel;
using StockManagementWebApi.Models.UserManagement;

namespace StockManagementWebApi.Services
{
	public interface IAuthService
	{
		Task<IReadOnlyList<UserList>> LoginAsync(Login data, CancellationToken cancellationToken = default);

		Task ResetPasswordAsync(ResetPassword data, CancellationToken cancellationToken = default);
	}

	/// <summary>
	/// Credential checks against the Sp_Login / Sp_ResetPassword stored procedures.
	/// Authentication failures surface as <see cref="UnauthorizedException"/> and a disabled
	/// account as <see cref="ForbiddenException"/>; everything else bubbles to the global
	/// middleware.
	/// </summary>
	public class AuthService : IAuthService
	{
		private readonly MydbContext _context;
		private readonly ILogger<AuthService> _logger;

		public AuthService(MydbContext context, ILogger<AuthService> logger)
		{
			_context = context;
			_logger = logger;
		}

		public async Task<IReadOnlyList<UserList>> LoginAsync(Login data, CancellationToken cancellationToken = default)
		{
			if (data == null)
			{
				throw new BadRequestException("Request data is null.");
			}

			var errors = new List<string>();
			if (string.IsNullOrWhiteSpace(data.Email))
			{
				errors.Add("Email is required.");
			}

			if (string.IsNullOrWhiteSpace(data.Password))
			{
				errors.Add("Password is required.");
			}

			if (errors.Count > 0)
			{
				throw new ValidationException(errors);
			}

			var users = await _context.UserLists
				.FromSqlRaw(@"exec Sp_Login @p0,@p1", data.Email, data.Password)
				.ToListAsync(cancellationToken);

			if (users.Count == 0)
			{
				// Deliberately not logging the supplied password, and not distinguishing
				// "unknown email" from "wrong password" in the response.
				_logger.LogWarning("Failed login attempt for {Email}.", data.Email);
				throw new UnauthorizedException("The User Email or Password is Invalid!!!");
			}

			if (users[0].IsActive == false)
			{
				_logger.LogWarning("Login attempt against the inactive account {Email}.", data.Email);
				throw new ForbiddenException("The User was InActive.. Please Contact Admin");
			}

			_logger.LogInformation("User {Email} signed in successfully.", data.Email);
			return users;
		}

		public async Task ResetPasswordAsync(ResetPassword data, CancellationToken cancellationToken = default)
		{
			if (data == null)
			{
				throw new BadRequestException("Request data is null.");
			}

			var errors = new List<string>();
			if (string.IsNullOrWhiteSpace(data.Email))
			{
				errors.Add("Email is required.");
			}

			if (string.IsNullOrWhiteSpace(data.ExistPassword))
			{
				errors.Add("Existing password is required.");
			}

			if (string.IsNullOrWhiteSpace(data.NewPassword))
			{
				errors.Add("New password is required.");
			}

			if (errors.Count > 0)
			{
				throw new ValidationException(errors);
			}

			var users = await _context.UserLists
				.FromSqlRaw(@"exec Sp_Login @p0,@p1", data.Email, data.ExistPassword)
				.ToListAsync(cancellationToken);

			// The original code indexed [0] directly, which threw when the credentials were wrong.
			// The empty check makes the intended rejection explicit instead of crashing.
			if (users.Count == 0 || users[0].Email != data.Email)
			{
				_logger.LogWarning("Password reset rejected for {Email}: credentials did not match.", data.Email);
				throw new UnauthorizedException("The User Email or Password is Invalid!!!");
			}

			await _context.Database.ExecuteSqlRawAsync(
				@"exec Sp_ResetPassword @p0,@p1,@p2",
				new object[] { data.Email, data.ExistPassword, data.NewPassword },
				cancellationToken);

			_logger.LogInformation("Password reset completed for {Email}.", data.Email);
		}
	}
}
