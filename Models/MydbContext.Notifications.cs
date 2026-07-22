using Microsoft.EntityFrameworkCore;
using StockManagementWebApi.Models.Notifications;

namespace StockManagementWebApi.Models;

/// <summary>Notification mappings. Called from OnModelCreatingPartial in MydbContext.Auth.cs.</summary>
public partial class MydbContext
{
	public virtual DbSet<Notification> Notifications { get; set; } = null!;

	public virtual DbSet<NotificationState> NotificationStates { get; set; } = null!;

	private static void ConfigureNotifications(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<Notification>(entity =>
		{
			entity.ToTable("sm_Notifications");

			entity.HasKey(e => e.Id);
			entity.Property(e => e.Id).HasColumnName("Pk_NotificationId");

			entity.Property(e => e.TenentCode).HasColumnName("Fk_TenentCode").HasMaxLength(10).IsRequired();
			entity.Property(e => e.NotificationType).HasMaxLength(50).IsRequired();
			entity.Property(e => e.Severity).HasMaxLength(20).IsRequired();
			entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
			entity.Property(e => e.Message).HasMaxLength(1000).IsRequired();
			entity.Property(e => e.MaterialNumber).HasMaxLength(100);
			entity.Property(e => e.SerialNumber).HasMaxLength(100);
			entity.Property(e => e.ReferenceType).HasMaxLength(50);
			entity.Property(e => e.ReferenceId).HasMaxLength(100);
			entity.Property(e => e.CreatedByUser).HasMaxLength(256);

			// The list is always "this tenant, newest first".
			entity.HasIndex(e => new { e.TenentCode, e.CreatedAtUtc });
		});

		modelBuilder.Entity<NotificationState>(entity =>
		{
			entity.ToTable("sm_NotificationStates");

			entity.HasKey(e => e.Id);
			entity.Property(e => e.Id).HasColumnName("Pk_NotificationStateId");

			entity.Property(e => e.NotificationId).HasColumnName("Fk_NotificationId");
			entity.Property(e => e.UserName).HasMaxLength(256).IsRequired();

			// One state row per user per notification.
			entity.HasIndex(e => new { e.NotificationId, e.UserName }).IsUnique();
			entity.HasIndex(e => e.UserName);
		});
	}
}
