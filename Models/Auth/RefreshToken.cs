namespace StockManagementWebApi.Models.Auth
{
	/// <summary>
	/// A refresh token issued to one signed-in session.
	/// </summary>
	/// <remarks>
	/// Only the SHA-256 hash of the token is stored. A leaked backup therefore cannot be
	/// replayed to mint access tokens, exactly as with password hashes.
	/// </remarks>
	public class RefreshToken
	{
		public long Id { get; set; }

		/// <summary>SHA-256 hash of the token value handed to the client.</summary>
		public string TokenHash { get; set; } = null!;

		/// <summary>Login id the token was issued to (matches sm_Users.LoginId).</summary>
		public string UserName { get; set; } = null!;

		public string? UserCode { get; set; }

		/// <summary>
		/// Snapshot of the claim values taken at sign-in.
		/// </summary>
		/// <remarks>
		/// The scaffolded sm_Users entity has drifted from the live schema, so refresh does not
		/// try to re-derive these from columns it cannot verify. They are refreshed from the
		/// UserList projection when that lookup succeeds, and fall back to this snapshot when it
		/// does not - which keeps refresh working rather than failing closed on a schema quirk.
		/// </remarks>
		public string? UserDisplayName { get; set; }

		public string? Email { get; set; }

		public string? UserType { get; set; }

		public string? AccessLevel { get; set; }

		public DateTime ExpiresAtUtc { get; set; }

		public DateTime CreatedAtUtc { get; set; }

		public string? CreatedByIp { get; set; }

		public DateTime? RevokedAtUtc { get; set; }

		public string? RevokedByIp { get; set; }

		/// <summary>Set when this token was rotated, pointing at its successor.</summary>
		public string? ReplacedByTokenHash { get; set; }

		/// <summary>Why the token stopped being usable - useful when auditing a session.</summary>
		public string? RevokedReason { get; set; }

		public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAtUtc;

		public bool IsRevoked => RevokedAtUtc != null;

		public bool IsActive(DateTime utcNow) => !IsRevoked && !IsExpired(utcNow);
	}
}
