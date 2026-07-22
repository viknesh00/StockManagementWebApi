using System.Text.Json;

namespace StockManagementWebApi.Common.Models
{
	/// <summary>
	/// Writes the standard envelope straight to an <see cref="HttpContext"/>, for the places
	/// that sit outside MVC and so cannot return an <c>IActionResult</c> - the exception
	/// middleware, the status-code fallback, and the JWT bearer events.
	/// </summary>
	public static class ApiResponseWriter
	{
		public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

		/// <summary>
		/// Replaces whatever was on the response with an error envelope. Does nothing if the
		/// response has already started - the caller should log that case instead.
		/// </summary>
		/// <param name="configureResponse">
		/// Runs after the response is reset but before the body is written - the only safe
		/// point at which to add extra headers.
		/// </param>
		public static async Task WriteErrorAsync(
			HttpContext context,
			int statusCode,
			string message,
			IEnumerable<string>? errors = null,
			DeveloperDetail? developer = null,
			Action<HttpResponse>? configureResponse = null)
		{
			if (context.Response.HasStarted)
			{
				return;
			}

			context.Response.Clear();
			context.Response.StatusCode = statusCode;
			context.Response.ContentType = "application/json; charset=utf-8";

			configureResponse?.Invoke(context.Response);

			var payload = ApiResponse.Fail(message, statusCode, context.TraceIdentifier, errors);
			payload.Developer = developer;

			await context.Response.WriteAsync(
				JsonSerializer.Serialize(payload, SerializerOptions),
				context.RequestAborted);
		}
	}
}
