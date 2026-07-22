using Microsoft.EntityFrameworkCore;
using StockManagementWebApi.Models.Auth;

namespace StockManagementWebApi.Models;

/// <summary>
/// Authentication-related mappings, kept in their own partial so the scaffolded
/// <see cref="MydbContext"/> file stays untouched and re-scaffoldable.
/// </summary>
public partial class MydbContext
{
	public virtual DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

	/// <summary>
	/// Single implementation of the scaffolded partial hook. Each feature area keeps its own
	/// configure method in its own file; add the call here when a new one appears.
	/// </summary>
	partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
	{
		ConfigureAuthentication(modelBuilder);
		ConfigureNotifications(modelBuilder);
	}

	private static void ConfigureAuthentication(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<RefreshToken>(entity =>
		{
			entity.ToTable("sm_RefreshTokens");

			entity.HasKey(e => e.Id);

			entity.Property(e => e.Id).HasColumnName("Pk_RefreshTokenId");

			entity.Property(e => e.TokenHash)
				.HasMaxLength(88)
				.IsUnicode(false)
				.IsRequired();

			entity.Property(e => e.UserName).HasMaxLength(256).IsRequired();
			entity.Property(e => e.UserCode).HasMaxLength(50);
			entity.Property(e => e.UserDisplayName).HasMaxLength(256);
			entity.Property(e => e.Email).HasMaxLength(256);
			entity.Property(e => e.UserType).HasMaxLength(50);
			entity.Property(e => e.AccessLevel).HasMaxLength(50);
			entity.Property(e => e.CreatedByIp).HasMaxLength(64);
			entity.Property(e => e.RevokedByIp).HasMaxLength(64);
			entity.Property(e => e.ReplacedByTokenHash).HasMaxLength(88).IsUnicode(false);
			entity.Property(e => e.RevokedReason).HasMaxLength(200);

			// Lookups always go through the hash, and it must be unique.
			entity.HasIndex(e => e.TokenHash).IsUnique();

			// Revoking every session for one user is a common admin action.
			entity.HasIndex(e => e.UserName);
		});
	}
}
