using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StockManagementWebApi.Common.Auth;
using StockManagementWebApi.Common.Exceptions;
using StockManagementWebApi.Models;
using StockManagementWebApi.Models.Auth;
using StockManagementWebApi.Models.LoginModel;
using StockManagementWebApi.Models.UserManagement;

namespace StockManagementWebApi.Services
{
	public interface IAuthService
	{
		Task<AuthResult> LoginAsync(Login data, string? clientIp, CancellationToken cancellationToken = default);

		Task<AuthResult> RefreshAsync(string? refreshToken, string? clientIp, CancellationToken cancellationToken = default);

		Task LogoutAsync(string? refreshToken, string? clientIp, CancellationToken cancellationToken = default);

		Task ResetPasswordAsync(ResetPassword data, CancellationToken cancellationToken = default);
	}

	/// <summary>
	/// Sign-in, token refresh and sign-out.
	/// </summary>
	/// <remarks>
	/// Credentials are still checked by the existing Sp_Login stored procedure - password
	/// handling is unchanged. What is new is that a successful check now mints a short-lived
	/// access token plus a rotating refresh token.
	/// </remarks>
	public class AuthService : IAuthService
	{
		private const string InvalidCredentialsMessage = "The User Email or Password is Invalid!!!";
		private const string InvalidRefreshTokenMessage = "Your session is no longer valid. Please sign in again.";

		private readonly MydbContext _context;
		private readonly ITokenService _tokenService;
		private readonly JwtOptions _jwtOptions;
		private readonly ILogger<AuthService> _logger;

		public AuthService(
			MydbContext context,
			ITokenService tokenService,
			IOptions<JwtOptions> jwtOptions,
			ILogger<AuthService> logger)
		{
			_context = context;
			_tokenService = tokenService;
			_jwtOptions = jwtOptions.Value;
			_logger = logger;
		}

		// ------------------------------------------------------------------ Sign in

		public async Task<AuthResult> LoginAsync(Login data, string? clientIp, CancellationToken cancellationToken = default)
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
				throw new UnauthorizedException(InvalidCredentialsMessage);
			}

			var user = users[0];

			if (user.IsActive == false)
			{
				_logger.LogWarning("Login attempt against the inactive account {Email}.", data.Email);
				throw new ForbiddenException("The User was InActive.. Please Contact Admin");
			}

			var result = await IssueTokensAsync(data.Email!, user, clientIp, cancellationToken);

			_logger.LogInformation("User {Email} signed in successfully.", data.Email);
			return result;
		}

		// ------------------------------------------------------------------ Refresh

		public async Task<AuthResult> RefreshAsync(string? refreshToken, string? clientIp, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrWhiteSpace(refreshToken))
			{
				throw new ValidationException("Refresh token is required.");
			}

			var now = DateTime.UtcNow;
			var tokenHash = _tokenService.HashRefreshToken(refreshToken);

			var stored = await _context.RefreshTokens
				.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

			if (stored == null)
			{
				_logger.LogWarning("Refresh rejected: token not recognised. Client IP {ClientIp}.", clientIp);
				throw new UnauthorizedException(InvalidRefreshTokenMessage);
			}

			if (stored.IsRevoked)
			{
				// A revoked token being presented means either a replayed request or a stolen
				// token. Either way the safe response is to end every session for that user.
				_logger.LogCritical(
					"Refresh token reuse detected for {UserName} from {ClientIp}. Revoking all sessions.",
					stored.UserName, clientIp);

				await RevokeAllForUserAsync(stored.UserName, clientIp, "Reuse of a revoked token detected", cancellationToken);

				throw new UnauthorizedException(InvalidRefreshTokenMessage);
			}

			if (stored.IsExpired(now))
			{
				_logger.LogInformation("Refresh rejected for {UserName}: token expired.", stored.UserName);
				throw new UnauthorizedException(InvalidRefreshTokenMessage);
			}

			if (!await IsAccountActiveAsync(stored.UserName, cancellationToken))
			{
				_logger.LogWarning("Refresh rejected for {UserName}: account is no longer active.", stored.UserName);

				await RevokeAllForUserAsync(stored.UserName, clientIp, "Account deactivated", cancellationToken);

				throw new ForbiddenException("The User was InActive.. Please Contact Admin");
			}

			var user = await ResolveUserAsync(stored, cancellationToken);

			// Rotate: the presented token is retired and replaced in the same transaction as
			// the new one is stored, so a crash cannot leave the user with neither.
			await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

			var rawRefreshToken = _tokenService.CreateRefreshToken();
			var newHash = _tokenService.HashRefreshToken(rawRefreshToken);

			stored.RevokedAtUtc = now;
			stored.RevokedByIp = clientIp;
			stored.RevokedReason = "Rotated on refresh";
			stored.ReplacedByTokenHash = newHash;

			_context.RefreshTokens.Add(NewRefreshTokenRow(newHash, stored.UserName, user, clientIp, now));

			await _context.SaveChangesAsync(cancellationToken);
			await transaction.CommitAsync(cancellationToken);

			var access = _tokenService.CreateAccessToken(stored.UserName, user);

			_logger.LogInformation("Access token refreshed for {UserName}.", stored.UserName);

			return new AuthResult
			{
				AccessToken = access.Token,
				RefreshToken = rawRefreshToken,
				ExpiresIn = access.ExpiresInSeconds,
				ExpiresAtUtc = access.ExpiresAtUtc,
				User = user
			};
		}

		// ------------------------------------------------------------------ Sign out

		public async Task LogoutAsync(string? refreshToken, string? clientIp, CancellationToken cancellationToken = default)
		{
			// Signing out must never fail the caller - the client is discarding its tokens
			// regardless. An unknown token is simply a no-op.
			if (string.IsNullOrWhiteSpace(refreshToken))
			{
				return;
			}

			var tokenHash = _tokenService.HashRefreshToken(refreshToken);

			var stored = await _context.RefreshTokens
				.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

			if (stored == null || stored.IsRevoked)
			{
				return;
			}

			stored.RevokedAtUtc = DateTime.UtcNow;
			stored.RevokedByIp = clientIp;
			stored.RevokedReason = "Signed out";

			await _context.SaveChangesAsync(cancellationToken);

			_logger.LogInformation("User {UserName} signed out.", stored.UserName);
		}

		// ------------------------------------------------------------------ Password reset

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
				throw new UnauthorizedException(InvalidCredentialsMessage);
			}

			await _context.Database.ExecuteSqlRawAsync(
				@"exec Sp_ResetPassword @p0,@p1,@p2",
				new object[] { data.Email!, data.ExistPassword!, data.NewPassword! },
				cancellationToken);

			// Changing a password invalidates every existing session.
			await RevokeAllForUserAsync(data.Email!, null, "Password changed", cancellationToken);

			_logger.LogInformation("Password reset completed for {Email}; all sessions revoked.", data.Email);
		}

		// ------------------------------------------------------------------ Helpers

		private async Task<AuthResult> IssueTokensAsync(string loginId, UserList user, string? clientIp, CancellationToken cancellationToken)
		{
			var now = DateTime.UtcNow;
			var rawRefreshToken = _tokenService.CreateRefreshToken();
			var tokenHash = _tokenService.HashRefreshToken(rawRefreshToken);

			_context.RefreshTokens.Add(NewRefreshTokenRow(tokenHash, loginId, user, clientIp, now));
			await _context.SaveChangesAsync(cancellationToken);

			var access = _tokenService.CreateAccessToken(loginId, user);

			return new AuthResult
			{
				AccessToken = access.Token,
				RefreshToken = rawRefreshToken,
				ExpiresIn = access.ExpiresInSeconds,
				ExpiresAtUtc = access.ExpiresAtUtc,
				User = user
			};
		}

		private RefreshToken NewRefreshTokenRow(string tokenHash, string loginId, UserList user, string? clientIp, DateTime now)
			=> new RefreshToken
			{
				TokenHash = tokenHash,
				UserName = loginId,
				UserCode = user.UserCode,
				UserDisplayName = user.UserName,
				Email = user.Email,
				UserType = user.UserType,
				AccessLevel = user.AccessLevel,
				CreatedAtUtc = now,
				CreatedByIp = clientIp,
				ExpiresAtUtc = now.AddDays(_jwtOptions.RefreshTokenDays)
			};

		/// <summary>
		/// Confirms the account still exists and is enabled. Uses only columns that the live
		/// schema is known to have, rather than the scaffolded entity, which has drifted.
		/// </summary>
		private async Task<bool> IsAccountActiveAsync(string loginId, CancellationToken cancellationToken)
		{
			var flags = await _context.Database
				.SqlQueryRaw<int>(
					"SELECT CAST(IsActive AS INT) AS Value FROM sm_Users WHERE LoginId = @p0",
					loginId)
				.ToListAsync(cancellationToken);

			return flags.Count > 0 && flags[0] == 1;
		}

		/// <summary>
		/// Rebuilds the user's claim values. Prefers the live UserList projection so role and
		/// access-level changes take effect on the next refresh; falls back to the snapshot
		/// captured at sign-in when the projection does not return the row.
		/// </summary>
		private async Task<UserList> ResolveUserAsync(RefreshToken stored, CancellationToken cancellationToken)
		{
			try
			{
				var tenantCode = await _context.Database
					.SqlQueryRaw<string>(
						"SELECT Fk_TenentCode AS Value FROM sm_Users WHERE LoginId = @p0",
						stored.UserName)
					.FirstOrDefaultAsync(cancellationToken);

				if (!string.IsNullOrWhiteSpace(tenantCode))
				{
					var tenantUsers = await _context.UserLists
						.FromSqlRaw(@"exec UserList @p0", tenantCode)
						.ToListAsync(cancellationToken);

					var match = tenantUsers.FirstOrDefault(u =>
						string.Equals(u.Email, stored.UserName, StringComparison.OrdinalIgnoreCase));

					if (match != null)
					{
						return match;
					}
				}
			}
			catch (Exception exception)
			{
				// A refresh must not fail because the lookup projection misbehaved; the
				// snapshot below is sufficient to re-issue an equivalent access token.
				_logger.LogWarning(
					exception,
					"Could not refresh claims for {UserName} from the UserList projection; using the sign-in snapshot.",
					stored.UserName);
			}

			return new UserList
			{
				UserCode = stored.UserCode,
				UserName = stored.UserDisplayName,
				Email = stored.Email,
				UserType = stored.UserType,
				AccessLevel = stored.AccessLevel,
				IsActive = true
			};
		}

		private async Task RevokeAllForUserAsync(string loginId, string? clientIp, string reason, CancellationToken cancellationToken)
		{
			var now = DateTime.UtcNow;

			var active = await _context.RefreshTokens
				.Where(t => t.UserName == loginId && t.RevokedAtUtc == null)
				.ToListAsync(cancellationToken);

			if (active.Count == 0)
			{
				return;
			}

			foreach (var token in active)
			{
				token.RevokedAtUtc = now;
				token.RevokedByIp = clientIp;
				token.RevokedReason = reason;
			}

			await _context.SaveChangesAsync(cancellationToken);

			_logger.LogInformation(
				"Revoked {Count} refresh token(s) for {UserName}: {Reason}.",
				active.Count, loginId, reason);
		}
	}
}
