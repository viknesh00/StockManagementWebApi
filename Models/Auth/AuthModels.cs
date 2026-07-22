using StockManagementWebApi.Models.UserManagement;

namespace StockManagementWebApi.Models.Auth
{
	/// <summary>What a successful sign-in or refresh returns inside the standard envelope's <c>data</c>.</summary>
	public class AuthResult
	{
		public string AccessToken { get; set; } = string.Empty;

		public string RefreshToken { get; set; } = string.Empty;

		public string TokenType { get; set; } = "Bearer";

		/// <summary>Access token lifetime in seconds - what the client should schedule a refresh against.</summary>
		public int ExpiresIn { get; set; }

		public DateTime ExpiresAtUtc { get; set; }

		/// <summary>The signed-in user. Same fields the login endpoint returned before tokens existed.</summary>
		public UserList User { get; set; } = new UserList();
	}

	/// <summary>Body of POST /api/Login/Refresh and POST /api/Login/Logout.</summary>
	public class RefreshTokenRequest
	{
		public string? RefreshToken { get; set; }
	}
}
