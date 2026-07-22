using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockManagementWebApi.Common.Controllers;
using StockManagementWebApi.Models.Auth;
using StockManagementWebApi.Models.LoginModel;
using StockManagementWebApi.Services;

namespace StockManagementWebApi.Controllers
{
	/// <summary>
	/// Sign-in, token refresh and sign-out. Every action here is anonymous by design - these
	/// are the endpoints a caller reaches when it has no valid access token.
	/// </summary>
	[Route("api/[controller]")]
	[AllowAnonymous]
	public class LoginController : BaseApiController
	{
		private readonly IAuthService _authService;

		public LoginController(IAuthService authService)
		{
			_authService = authService;
		}

		[HttpPost("Login")]
		public async Task<IActionResult> Login([FromBody] Login data, CancellationToken cancellationToken)
		{
			var result = await _authService.LoginAsync(data, ClientIpAddress, cancellationToken);

			return Success(result, "Login successful.");
		}

		/// <summary>
		/// Exchanges a valid refresh token for a new access/refresh pair. The presented refresh
		/// token is retired in the same operation, so each one is usable exactly once.
		/// </summary>
		[HttpPost("Refresh")]
		public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
		{
			var result = await _authService.RefreshAsync(request?.RefreshToken, ClientIpAddress, cancellationToken);

			return Success(result, "Token refreshed successfully.");
		}

		/// <summary>
		/// Revokes the supplied refresh token. Always succeeds - the client is discarding its
		/// tokens either way, and an unknown token is treated as already signed out.
		/// </summary>
		[HttpPost("Logout")]
		public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
		{
			await _authService.LogoutAsync(request?.RefreshToken, ClientIpAddress, cancellationToken);

			return Success(message: "Signed out successfully.");
		}

		[HttpPost("ResetPassword")]
		public async Task<IActionResult> ResetPassword([FromBody] ResetPassword data, CancellationToken cancellationToken)
		{
			await _authService.ResetPasswordAsync(data, cancellationToken);

			return Success(message: "Password reset successfully.");
		}
	}
}
