namespace StockManagementWebApi.Models.Notifications
{
	/// <summary>A notification as the signed-in user sees it, with their own read state folded in.</summary>
	public class NotificationListItem
	{
		public long Id { get; set; }

		public string NotificationType { get; set; } = string.Empty;

		public string Severity { get; set; } = string.Empty;

		public string Title { get; set; } = string.Empty;

		public string Message { get; set; } = string.Empty;

		public string? MaterialNumber { get; set; }

		public string? SerialNumber { get; set; }

		public string? ReferenceType { get; set; }

		public string? ReferenceId { get; set; }

		public string? CreatedByUser { get; set; }

		public DateTime CreatedAtUtc { get; set; }

		public bool IsRead { get; set; }

		public DateTime? ReadAtUtc { get; set; }
	}

	/// <summary>One page of notifications plus the counts the UI shows in its tabs and badge.</summary>
	public class NotificationPage
	{
		public IReadOnlyList<NotificationListItem> Items { get; set; } = Array.Empty<NotificationListItem>();

		public int Page { get; set; }

		public int PageSize { get; set; }

		/// <summary>Total matching the current filter, for pagination.</summary>
		public int TotalCount { get; set; }

		/// <summary>Unread total ignoring the current filter, for the bell badge.</summary>
		public int UnreadCount { get; set; }
	}

	/// <summary>Query options for the notification list.</summary>
	public class NotificationQuery
	{
		/// <summary>"all", "unread" or "read". Anything else is treated as "all".</summary>
		public string? Status { get; set; }

		/// <summary>Restrict to one <see cref="NotificationTypes"/> value.</summary>
		public string? Type { get; set; }

		/// <summary>Free-text match over title, message, material and serial number.</summary>
		public string? Search { get; set; }

		public int Page { get; set; } = 1;

		public int PageSize { get; set; } = 20;
	}

	/// <summary>Unread badge payload.</summary>
	public class UnreadCountResult
	{
		public int UnreadCount { get; set; }
	}
}
