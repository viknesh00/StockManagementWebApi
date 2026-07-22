using System.Text.Json.Serialization;

namespace StockManagementWebApi.Common.Models
{
	/// <summary>
	/// The single envelope every endpoint in this API returns, for both success and failure.
	/// Property declaration order is also the JSON serialization order.
	/// </summary>
	public class ApiResponse<T>
	{
		public bool Success { get; set; }

		public int StatusCode { get; set; }

		public string Message { get; set; } = string.Empty;

		public T? Data { get; set; }

		public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();

		public string TraceId { get; set; } = string.Empty;

		public DateTime Timestamp { get; set; } = DateTime.UtcNow;

		/// <summary>
		/// Diagnostic detail (exception type, message and stack trace). Populated only in the
		/// Development environment; omitted from the payload entirely everywhere else.
		/// </summary>
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public DeveloperDetail? Developer { get; set; }
	}

	/// <summary>Non-generic envelope, used wherever the payload type is not known statically.</summary>
	public class ApiResponse : ApiResponse<object>
	{
		public static ApiResponse Ok(object? data, string message, int statusCode, string traceId) => new ApiResponse
		{
			Success = true,
			StatusCode = statusCode,
			Message = message,
			Data = data,
			Errors = Array.Empty<string>(),
			TraceId = traceId,
			Timestamp = DateTime.UtcNow
		};

		public static ApiResponse Fail(string message, int statusCode, string traceId, IEnumerable<string>? errors = null) => new ApiResponse
		{
			Success = false,
			StatusCode = statusCode,
			Message = message,
			Data = null,
			Errors = errors?.ToList() ?? (IReadOnlyList<string>)Array.Empty<string>(),
			TraceId = traceId,
			Timestamp = DateTime.UtcNow
		};
	}

	/// <summary>Development-only diagnostic block. Never emitted outside Development.</summary>
	public class DeveloperDetail
	{
		public string ExceptionType { get; set; } = string.Empty;

		public string ExceptionMessage { get; set; } = string.Empty;

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string? InnerExceptionMessage { get; set; }

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string? StackTrace { get; set; }
	}
}
