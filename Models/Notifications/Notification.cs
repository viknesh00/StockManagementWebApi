namespace StockManagementWebApi.Models.Notifications
{
	/// <summary>
	/// One notifiable event, scoped to a tenant rather than to a person.
	/// </summary>
	/// <remarks>
	/// Read and dismissed state lives in <see cref="NotificationState"/>, one row per user who
	/// has actually interacted with the notification. Storing the event once and the state
	/// separately avoids fanning a single bulk import out into a row per user, which is what a
	/// per-recipient table would do.
	/// </remarks>
	public class Notification
	{
		public long Id { get; set; }

		/// <summary>Tenant the event belongs to; everyone in it can see the notification.</summary>
		public string TenentCode { get; set; } = null!;

		public string NotificationType { get; set; } = NotificationTypes.System;

		public string Severity { get; set; } = NotificationSeverities.Info;

		public string Title { get; set; } = null!;

		public string Message { get; set; } = null!;

		public string? MaterialNumber { get; set; }

		public string? SerialNumber { get; set; }

		/// <summary>What the notification points at, so the UI can deep-link to it.</summary>
		public string? ReferenceType { get; set; }

		public string? ReferenceId { get; set; }

		/// <summary>Login id of whoever performed the action.</summary>
		public string? CreatedByUser { get; set; }

		public DateTime CreatedAtUtc { get; set; }
	}

	/// <summary>Per-user read/dismissed state for a notification.</summary>
	public class NotificationState
	{
		public long Id { get; set; }

		public long NotificationId { get; set; }

		public string UserName { get; set; } = null!;

		public DateTime? ReadAtUtc { get; set; }

		/// <summary>Set when the user clears the notification. Dismissed rows are hidden from them only.</summary>
		public DateTime? DismissedAtUtc { get; set; }
	}

	/// <summary>Notification categories. Strings rather than an enum so the DB stays readable.</summary>
	public static class NotificationTypes
	{
		public const string StockInward = "StockInward";
		public const string StockOutward = "StockOutward";
		public const string StockReturn = "StockReturn";
		public const string StockDeleted = "StockDeleted";
		public const string Material = "Material";
		public const string System = "System";

		public static readonly string[] All =
		{
			StockInward, StockOutward, StockReturn, StockDeleted, Material, System
		};

		public static bool IsValid(string? value) =>
			!string.IsNullOrWhiteSpace(value) &&
			All.Contains(value, StringComparer.OrdinalIgnoreCase);
	}

	/// <summary>How prominently the UI should present a notification.</summary>
	public static class NotificationSeverities
	{
		public const string Info = "Info";
		public const string Success = "Success";
		public const string Warning = "Warning";
		public const string Critical = "Critical";

		public static readonly string[] All = { Info, Success, Warning, Critical };
	}
}
