using Microsoft.EntityFrameworkCore;
using StockManagementWebApi.Common.Exceptions;
using StockManagementWebApi.Models;
using StockManagementWebApi.Models.Notifications;

namespace StockManagementWebApi.Services
{
	public interface INotificationService
	{
		Task<NotificationPage> GetAsync(string userName, NotificationQuery query, CancellationToken cancellationToken = default);

		Task<int> GetUnreadCountAsync(string userName, CancellationToken cancellationToken = default);

		Task MarkAsReadAsync(string userName, long notificationId, CancellationToken cancellationToken = default);

		Task<int> MarkAllAsReadAsync(string userName, CancellationToken cancellationToken = default);

		Task DismissAsync(string userName, long notificationId, CancellationToken cancellationToken = default);

		Task<int> ClearAllAsync(string userName, CancellationToken cancellationToken = default);
	}

	/// <summary>
	/// Reads and mutates the signed-in user's notification feed.
	/// </summary>
	/// <remarks>
	/// Every method is scoped by the caller's tenant, resolved from their login id - a
	/// notification id from another tenant simply will not be found.
	/// </remarks>
	public class NotificationService : INotificationService
	{
		private const int MaxPageSize = 100;

		private readonly MydbContext _context;
		private readonly ILogger<NotificationService> _logger;

		public NotificationService(MydbContext context, ILogger<NotificationService> logger)
		{
			_context = context;
			_logger = logger;
		}

		public async Task<NotificationPage> GetAsync(string userName, NotificationQuery query, CancellationToken cancellationToken = default)
		{
			Require(userName, nameof(userName));
			query ??= new NotificationQuery();

			var tenantCode = await ResolveTenantAsync(userName, cancellationToken);

			var page = query.Page < 1 ? 1 : query.Page;
			var pageSize = query.PageSize is < 1 or > MaxPageSize ? 20 : query.PageSize;

			// Left join the per-user state so one query answers "what is in my feed, and
			// which of it have I already read".
			var feed =
				from notification in _context.Notifications.AsNoTracking()
				where notification.TenentCode == tenantCode
				join state in _context.NotificationStates.AsNoTracking()
						.Where(s => s.UserName == userName)
					on notification.Id equals state.NotificationId into states
				from state in states.DefaultIfEmpty()
				where state == null || state.DismissedAtUtc == null
				select new { notification, state };

			// The badge count ignores the filters below - it is "everything unread".
			var unreadCount = await feed.CountAsync(x => x.state == null || x.state.ReadAtUtc == null, cancellationToken);

			if (!string.IsNullOrWhiteSpace(query.Type) && NotificationTypes.IsValid(query.Type))
			{
				feed = feed.Where(x => x.notification.NotificationType == query.Type);
			}

			var status = (query.Status ?? "all").Trim().ToLowerInvariant();
			feed = status switch
			{
				"unread" => feed.Where(x => x.state == null || x.state.ReadAtUtc == null),
				"read" => feed.Where(x => x.state != null && x.state.ReadAtUtc != null),
				_ => feed
			};

			if (!string.IsNullOrWhiteSpace(query.Search))
			{
				var term = query.Search.Trim();
				feed = feed.Where(x =>
					EF.Functions.Like(x.notification.Title, $"%{term}%") ||
					EF.Functions.Like(x.notification.Message, $"%{term}%") ||
					(x.notification.MaterialNumber != null && EF.Functions.Like(x.notification.MaterialNumber, $"%{term}%")) ||
					(x.notification.SerialNumber != null && EF.Functions.Like(x.notification.SerialNumber, $"%{term}%")));
			}

			var totalCount = await feed.CountAsync(cancellationToken);

			var items = await feed
				.OrderByDescending(x => x.notification.CreatedAtUtc)
				.ThenByDescending(x => x.notification.Id)
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.Select(x => new NotificationListItem
				{
					Id = x.notification.Id,
					NotificationType = x.notification.NotificationType,
					Severity = x.notification.Severity,
					Title = x.notification.Title,
					Message = x.notification.Message,
					MaterialNumber = x.notification.MaterialNumber,
					SerialNumber = x.notification.SerialNumber,
					ReferenceType = x.notification.ReferenceType,
					ReferenceId = x.notification.ReferenceId,
					CreatedByUser = x.notification.CreatedByUser,
					CreatedAtUtc = x.notification.CreatedAtUtc,
					IsRead = x.state != null && x.state.ReadAtUtc != null,
					ReadAtUtc = x.state == null ? null : x.state.ReadAtUtc
				})
				.ToListAsync(cancellationToken);

			return new NotificationPage
			{
				Items = items,
				Page = page,
				PageSize = pageSize,
				TotalCount = totalCount,
				UnreadCount = unreadCount
			};
		}

		public async Task<int> GetUnreadCountAsync(string userName, CancellationToken cancellationToken = default)
		{
			Require(userName, nameof(userName));

			var tenantCode = await ResolveTenantAsync(userName, cancellationToken);

			return await (
				from notification in _context.Notifications.AsNoTracking()
				where notification.TenentCode == tenantCode
				join state in _context.NotificationStates.AsNoTracking()
						.Where(s => s.UserName == userName)
					on notification.Id equals state.NotificationId into states
				from state in states.DefaultIfEmpty()
				where state == null || (state.DismissedAtUtc == null && state.ReadAtUtc == null)
				select notification.Id
			).CountAsync(cancellationToken);
		}

		public async Task MarkAsReadAsync(string userName, long notificationId, CancellationToken cancellationToken = default)
		{
			Require(userName, nameof(userName));

			var notification = await FindInTenantAsync(userName, notificationId, cancellationToken);
			var state = await GetOrCreateStateAsync(userName, notification.Id, cancellationToken);

			if (state.ReadAtUtc == null)
			{
				state.ReadAtUtc = DateTime.UtcNow;
				await _context.SaveChangesAsync(cancellationToken);
			}
		}

		public async Task<int> MarkAllAsReadAsync(string userName, CancellationToken cancellationToken = default)
		{
			Require(userName, nameof(userName));

			var tenantCode = await ResolveTenantAsync(userName, cancellationToken);
			var now = DateTime.UtcNow;

			var unread = await (
				from notification in _context.Notifications
				where notification.TenentCode == tenantCode
				join state in _context.NotificationStates.Where(s => s.UserName == userName)
					on notification.Id equals state.NotificationId into states
				from state in states.DefaultIfEmpty()
				where state == null || (state.DismissedAtUtc == null && state.ReadAtUtc == null)
				select new { NotificationId = notification.Id, State = state }
			).ToListAsync(cancellationToken);

			if (unread.Count == 0)
			{
				return 0;
			}

			foreach (var row in unread)
			{
				if (row.State == null)
				{
					_context.NotificationStates.Add(new NotificationState
					{
						NotificationId = row.NotificationId,
						UserName = userName,
						ReadAtUtc = now
					});
				}
				else
				{
					row.State.ReadAtUtc = now;
				}
			}

			await _context.SaveChangesAsync(cancellationToken);

			_logger.LogInformation("Marked {Count} notification(s) read for {UserName}.", unread.Count, userName);
			return unread.Count;
		}

		public async Task DismissAsync(string userName, long notificationId, CancellationToken cancellationToken = default)
		{
			Require(userName, nameof(userName));

			var notification = await FindInTenantAsync(userName, notificationId, cancellationToken);
			var state = await GetOrCreateStateAsync(userName, notification.Id, cancellationToken);

			var now = DateTime.UtcNow;
			state.DismissedAtUtc ??= now;
			// Dismissing also settles the unread count.
			state.ReadAtUtc ??= now;

			await _context.SaveChangesAsync(cancellationToken);
		}

		public async Task<int> ClearAllAsync(string userName, CancellationToken cancellationToken = default)
		{
			Require(userName, nameof(userName));

			var tenantCode = await ResolveTenantAsync(userName, cancellationToken);
			var now = DateTime.UtcNow;

			var visible = await (
				from notification in _context.Notifications
				where notification.TenentCode == tenantCode
				join state in _context.NotificationStates.Where(s => s.UserName == userName)
					on notification.Id equals state.NotificationId into states
				from state in states.DefaultIfEmpty()
				where state == null || state.DismissedAtUtc == null
				select new { NotificationId = notification.Id, State = state }
			).ToListAsync(cancellationToken);

			if (visible.Count == 0)
			{
				return 0;
			}

			foreach (var row in visible)
			{
				if (row.State == null)
				{
					_context.NotificationStates.Add(new NotificationState
					{
						NotificationId = row.NotificationId,
						UserName = userName,
						ReadAtUtc = now,
						DismissedAtUtc = now
					});
				}
				else
				{
					row.State.ReadAtUtc ??= now;
					row.State.DismissedAtUtc ??= now;
				}
			}

			await _context.SaveChangesAsync(cancellationToken);

			_logger.LogInformation("Cleared {Count} notification(s) for {UserName}.", visible.Count, userName);
			return visible.Count;
		}

		// ------------------------------------------------------------------ Helpers

		private async Task<Notification> FindInTenantAsync(string userName, long notificationId, CancellationToken cancellationToken)
		{
			var tenantCode = await ResolveTenantAsync(userName, cancellationToken);

			var notification = await _context.Notifications
				.FirstOrDefaultAsync(n => n.Id == notificationId && n.TenentCode == tenantCode, cancellationToken);

			// Scoping the lookup by tenant means an id from another tenant is indistinguishable
			// from one that does not exist.
			return notification ?? throw NotFoundException.For("Notification", notificationId);
		}

		private async Task<NotificationState> GetOrCreateStateAsync(string userName, long notificationId, CancellationToken cancellationToken)
		{
			var state = await _context.NotificationStates
				.FirstOrDefaultAsync(s => s.NotificationId == notificationId && s.UserName == userName, cancellationToken);

			if (state != null)
			{
				return state;
			}

			state = new NotificationState { NotificationId = notificationId, UserName = userName };
			_context.NotificationStates.Add(state);
			return state;
		}

		private async Task<string> ResolveTenantAsync(string userName, CancellationToken cancellationToken)
		{
			var tenantCode = await _context.Database
				.SqlQueryRaw<string>("SELECT Fk_TenentCode AS Value FROM sm_Users WHERE LoginId = @p0", userName)
				.FirstOrDefaultAsync(cancellationToken);

			if (string.IsNullOrWhiteSpace(tenantCode))
			{
				throw new NotFoundException($"No tenant is associated with the user '{userName}'.");
			}

			return tenantCode;
		}

		private static void Require(string value, string parameterName)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				throw new ValidationException($"{parameterName} is required.");
			}
		}
	}
}
