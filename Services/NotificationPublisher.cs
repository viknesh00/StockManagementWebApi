using Microsoft.EntityFrameworkCore;
using StockManagementWebApi.Models;
using StockManagementWebApi.Models.Notifications;

namespace StockManagementWebApi.Services
{
	/// <summary>
	/// Raises notifications from business operations.
	/// </summary>
	/// <remarks>
	/// Every method here is best-effort and swallows its own exceptions. A notification is a
	/// side effect of a stock operation, never a precondition of it - failing to record one
	/// must not roll back an inward, an outward or a return.
	/// </remarks>
	public interface INotificationPublisher
	{
		/// <param name="actingUser">
		/// Login id of whoever performed the action. Pass null when the operation does not carry
		/// one - the publisher then falls back to the authenticated caller's identity.
		/// </param>
		Task PublishAsync(
			string? actingUser,
			string notificationType,
			string severity,
			string title,
			string message,
			string? materialNumber = null,
			string? serialNumber = null,
			string? referenceType = null,
			string? referenceId = null,
			CancellationToken cancellationToken = default);
	}

	public class NotificationPublisher : INotificationPublisher
	{
		private readonly IServiceScopeFactory _scopeFactory;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly ILogger<NotificationPublisher> _logger;

		public NotificationPublisher(
			IServiceScopeFactory scopeFactory,
			IHttpContextAccessor httpContextAccessor,
			ILogger<NotificationPublisher> logger)
		{
			_scopeFactory = scopeFactory;
			_httpContextAccessor = httpContextAccessor;
			_logger = logger;
		}

		public async Task PublishAsync(
			string? actingUser,
			string notificationType,
			string severity,
			string title,
			string message,
			string? materialNumber = null,
			string? serialNumber = null,
			string? referenceType = null,
			string? referenceId = null,
			CancellationToken cancellationToken = default)
		{
			try
			{
				// Several operations identify the user in their payload; the rest only have the
				// access token. Prefer the explicit value, fall back to the token.
				var user = string.IsNullOrWhiteSpace(actingUser)
					? _httpContextAccessor.HttpContext?.User?.Identity?.Name
					: actingUser;

				if (string.IsNullOrWhiteSpace(user))
				{
					return;
				}

				actingUser = user;

				// A dedicated scope, and therefore a dedicated DbContext. Sharing the caller's
				// context would mean this SaveChangesAsync also flushed whatever entity changes
				// the business operation happened to be tracking - several stock services mutate
				// tracked rows and then persist them with explicit SQL, so an early flush would
				// silently change what they write.
				await using var scope = _scopeFactory.CreateAsyncScope();
				var context = scope.ServiceProvider.GetRequiredService<MydbContext>();

				var tenantCode = await context.Database
					.SqlQueryRaw<string>("SELECT Fk_TenentCode AS Value FROM sm_Users WHERE LoginId = @p0", actingUser)
					.FirstOrDefaultAsync(cancellationToken);

				if (string.IsNullOrWhiteSpace(tenantCode))
				{
					_logger.LogDebug("Skipping notification: no tenant found for {UserName}.", actingUser);
					return;
				}

				context.Notifications.Add(new Notification
				{
					TenentCode = tenantCode,
					NotificationType = notificationType,
					Severity = severity,
					Title = Truncate(title, 200),
					Message = Truncate(message, 1000),
					MaterialNumber = Truncate(materialNumber, 100),
					SerialNumber = Truncate(serialNumber, 100),
					ReferenceType = Truncate(referenceType, 50),
					ReferenceId = Truncate(referenceId, 100),
					CreatedByUser = Truncate(actingUser, 256),
					CreatedAtUtc = DateTime.UtcNow
				});

				await context.SaveChangesAsync(cancellationToken);
			}
			catch (Exception exception)
			{
				// Never let a notification failure surface to the caller. Logged at Warning
				// because it is a real defect worth investigating, just not a request failure.
				_logger.LogWarning(
					exception,
					"Could not record a {NotificationType} notification for {UserName}. The business operation was unaffected.",
					notificationType, actingUser);
			}
		}

		private static string? Truncate(string? value, int maxLength) =>
			string.IsNullOrEmpty(value) || value.Length <= maxLength
				? value
				: value[..maxLength];
	}
}
