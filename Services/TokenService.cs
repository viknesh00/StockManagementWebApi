using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StockManagementWebApi.Common.Auth;
using StockManagementWebApi.Models.UserManagement;

namespace StockManagementWebApi.Services
{
	public interface ITokenService
	{
		/// <summary>Mints a signed access token for <paramref name="user"/>.</summary>
		(string Token, DateTime ExpiresAtUtc, int ExpiresInSeconds) CreateAccessToken(string loginId, UserList user);

		/// <summary>Generates a cryptographically random refresh token (the raw value the client keeps).</summary>
		string CreateRefreshToken();

		/// <summary>SHA-256 of a refresh token, base64 encoded. Only the hash is ever persisted.</summary>
		string HashRefreshToken(string refreshToken);
	}

	/// <summary>
	/// Issues JWT access tokens and opaque refresh tokens.
	/// </summary>
	/// <remarks>
	/// Access tokens are self-contained and short-lived, so nothing is looked up per request.
	/// Refresh tokens are deliberately opaque random strings rather than JWTs - they must be
	/// revocable, which means a database round trip anyway.
	/// </remarks>
	public class TokenService : ITokenService
	{
		/// <summary>Claim carrying the user's access level, alongside the standard role claim.</summary>
		public const string AccessLevelClaim = "access_level";

		/// <summary>Claim carrying the sm_Users primary key.</summary>
		public const string UserCodeClaim = "user_code";

		private readonly JwtOptions _options;
		private readonly SigningCredentials _signingCredentials;

		public TokenService(IOptions<JwtOptions> options)
		{
			_options = options.Value;

			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
			_signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
		}

		public (string Token, DateTime ExpiresAtUtc, int ExpiresInSeconds) CreateAccessToken(string loginId, UserList user)
		{
			var issuedAt = DateTime.UtcNow;
			var expiresAt = issuedAt.AddMinutes(_options.AccessTokenMinutes);

			var claims = new List<Claim>
			{
				// The login id is what every downstream endpoint means by "UserName".
				new Claim(JwtRegisteredClaimNames.Sub, loginId),
				new Claim(ClaimTypes.Name, loginId),
				new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
				new Claim(JwtRegisteredClaimNames.Iat,
					new DateTimeOffset(issuedAt).ToUnixTimeSeconds().ToString(),
					ClaimValueTypes.Integer64)
			};

			if (!string.IsNullOrWhiteSpace(user.UserCode))
			{
				claims.Add(new Claim(UserCodeClaim, user.UserCode));
				claims.Add(new Claim(ClaimTypes.NameIdentifier, user.UserCode));
			}

			if (!string.IsNullOrWhiteSpace(user.Email))
			{
				claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));
			}

			if (!string.IsNullOrWhiteSpace(user.UserType))
			{
				// Standard role claim, so [Authorize(Roles = "...")] works without extra wiring.
				claims.Add(new Claim(ClaimTypes.Role, user.UserType));
			}

			if (!string.IsNullOrWhiteSpace(user.AccessLevel))
			{
				claims.Add(new Claim(AccessLevelClaim, user.AccessLevel));
			}

			var token = new JwtSecurityToken(
				issuer: _options.Issuer,
				audience: _options.Audience,
				claims: claims,
				notBefore: issuedAt,
				expires: expiresAt,
				signingCredentials: _signingCredentials);

			return (
				new JwtSecurityTokenHandler().WriteToken(token),
				expiresAt,
				_options.AccessTokenMinutes * 60);
		}

		public string CreateRefreshToken()
		{
			// 256 bits of entropy, URL-safe so it survives being put in a header or JSON body.
			var bytes = RandomNumberGenerator.GetBytes(32);
			return Convert.ToBase64String(bytes)
				.TrimEnd('=')
				.Replace('+', '-')
				.Replace('/', '_');
		}

		public string HashRefreshToken(string refreshToken)
		{
			var hash = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
			return Convert.ToBase64String(hash);
		}
	}
}
