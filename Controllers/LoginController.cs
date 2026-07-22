using Microsoft.AspNetCore.Mvc;
using StockManagementWebApi.Common.Controllers;
using StockManagementWebApi.Models.LoginModel;
using StockManagementWebApi.Services;

namespace StockManagementWebApi.Controllers
{
	[Route("api/[controller]")]
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
			var users = await _authService.LoginAsync(data, cancellationToken);

			return Success(users, "Login successful.");
		}

		[HttpPost("ResetPassword")]
		public async Task<IActionResult> ResetPassword([FromBody] ResetPassword data, CancellationToken cancellationToken)
		{
			await _authService.ResetPasswordAsync(data, cancellationToken);

			return Success(message: "Password reset successfully.");
		}
	}
}
