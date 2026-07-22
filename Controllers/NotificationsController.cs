using Microsoft.AspNetCore.Mvc;
using StockManagementWebApi.Common.Controllers;
using StockManagementWebApi.Common.Exceptions;
using StockManagementWebApi.Models.Notifications;
using StockManagementWebApi.Services;

namespace StockManagementWebApi.Controllers
{
	/// <summary>
	/// The signed-in user's notification feed.
	/// </summary>
	/// <remarks>
	/// Unlike the older controllers, these endpoints take the user from the access token rather
	/// than from a route parameter. There is no existing client contract to preserve here, so
	/// there is no reason to let a caller name someone else.
	/// </remarks>
	[Route("api/[controller]")]
	public class NotificationsController : BaseApiController
	{
		private readonly INotificationService _notificationService;

		public NotificationsController(INotificationService notificationService)
		{
			_notificationService = notificationService;
		}

		/// <summary>GET api/Notifications?status=unread&amp;type=StockInward&amp;search=&amp;page=1&amp;pageSize=20</summary>
		[HttpGet]
		public async Task<IActionResult> GetNotifications(
			[FromQuery] string? status,
			[FromQuery] string? type,
			[FromQuery] string? search,
			[FromQuery] int page = 1,
			[FromQuery] int pageSize = 20,
			CancellationToken cancellationToken = default)
		{
			var result = await _notificationService.GetAsync(
				RequireCurrentUser(),
				new NotificationQuery
				{
					Status = status,
					Type = type,
					Search = search,
					Page = page,
					PageSize = pageSize
				},
				cancellationToken);

			return Success(result, "Notifications retrieved successfully.");
		}

		/// <summary>GET api/Notifications/unread-count - drives the sidebar badge.</summary>
		[HttpGet("unread-count")]
		public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
		{
			var count = await _notificationService.GetUnreadCountAsync(RequireCurrentUser(), cancellationToken);

			return Success(new UnreadCountResult { UnreadCount = count }, "Unread count retrieved successfully.");
		}

		/// <summary>POST api/Notifications/{id}/read</summary>
		[HttpPost("{id:long}/read")]
		public async Task<IActionResult> MarkAsRead(long id, CancellationToken cancellationToken)
		{
			await _notificationService.MarkAsReadAsync(RequireCurrentUser(), id, cancellationToken);

			return Updated(message: "Notification marked as read.");
		}

		/// <summary>POST api/Notifications/read-all</summary>
		[HttpPost("read-all")]
		public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
		{
			var affected = await _notificationService.MarkAllAsReadAsync(RequireCurrentUser(), cancellationToken);

			return Updated(new { markedAsRead = affected }, "All notifications marked as read.");
		}

		/// <summary>POST api/Notifications/{id}/dismiss - hides it for this user only.</summary>
		[HttpPost("{id:long}/dismiss")]
		public async Task<IActionResult> Dismiss(long id, CancellationToken cancellationToken)
		{
			await _notificationService.DismissAsync(RequireCurrentUser(), id, cancellationToken);

			return Deleted(message: "Notification dismissed.");
		}

		/// <summary>POST api/Notifications/clear - hides every current notification for this user.</summary>
		[HttpPost("clear")]
		public async Task<IActionResult> ClearAll(CancellationToken cancellationToken)
		{
			var affected = await _notificationService.ClearAllAsync(RequireCurrentUser(), cancellationToken);

			return Deleted(new { cleared = affected }, "All notifications cleared.");
		}

		/// <summary>
		/// The caller's login id. The global authorize filter should make this impossible to
		/// reach anonymously; the guard turns a misconfiguration into a clear 401 rather than a
		/// null-reference 500.
		/// </summary>
		private string RequireCurrentUser() =>
			CurrentUserName
			?? throw new UnauthorizedException("Authentication is required to access this resource.");
	}
}
