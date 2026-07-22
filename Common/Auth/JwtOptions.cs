using System.ComponentModel.DataAnnotations;

namespace StockManagementWebApi.Common.Auth
{
	/// <summary>Signing and lifetime settings for issued tokens, bound from the "Jwt" section.</summary>
	public class JwtOptions
	{
		public const string SectionName = "Jwt";

		[Required]
		public string Issuer { get; set; } = string.Empty;

		[Required]
		public string Audience { get; set; } = string.Empty;

		/// <summary>
		/// HMAC-SHA256 signing key. Must be at least 32 characters - anything shorter is
		/// rejected at startup rather than producing weak signatures.
		/// </summary>
		[Required]
		[MinLength(32, ErrorMessage = "Jwt:Key must be at least 32 characters for HMAC-SHA256.")]
		public string Key { get; set; } = string.Empty;

		/// <summary>How long an access token stays valid. Short by design; the refresh token covers the gap.</summary>
		[Range(1, 1440)]
		public int AccessTokenMinutes { get; set; } = 15;

		/// <summary>How long a refresh token stays valid, i.e. the maximum idle time before re-login.</summary>
		[Range(1, 365)]
		public int RefreshTokenDays { get; set; } = 7;

		/// <summary>
		/// Leeway applied when checking token expiry. Defaults to zero so an expired token is
		/// rejected the moment it expires, instead of the 5-minute default skew.
		/// </summary>
		[Range(0, 300)]
		public int ClockSkewSeconds { get; set; } = 0;
	}
}
