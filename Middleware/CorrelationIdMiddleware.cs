namespace StockManagementWebApi.Middleware
{
	/// <summary>
	/// Gives every request a stable correlation id: either the one the caller supplied via the
	/// <c>X-Correlation-ID</c> header, or a freshly generated one. The id becomes the request's
	/// <see cref="HttpContext.TraceIdentifier"/>, is echoed back on the response, and is attached
	/// to every log entry written while the request is in flight.
	/// </summary>
	public class CorrelationIdMiddleware
	{
		public const string HeaderName = "X-Correlation-ID";

		private readonly RequestDelegate _next;
		private readonly ILogger<CorrelationIdMiddleware> _logger;

		public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
		{
			_next = next;
			_logger = logger;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			var correlationId = ResolveCorrelationId(context);
			context.TraceIdentifier = correlationId;

			context.Response.OnStarting(() =>
			{
				context.Response.Headers[HeaderName] = correlationId;
				return Task.CompletedTask;
			});

			using (_logger.BeginScope(new Dictionary<string, object>
			{
				["CorrelationId"] = correlationId,
				["RequestPath"] = context.Request.Path.ToString(),
				["RequestMethod"] = context.Request.Method
			}))
			{
				await _next(context);
			}
		}

		private static string ResolveCorrelationId(HttpContext context)
		{
			if (context.Request.Headers.TryGetValue(HeaderName, out var supplied))
			{
				var candidate = supplied.ToString();
				// Cap the length so a hostile header cannot bloat every log line.
				if (!string.IsNullOrWhiteSpace(candidate) && candidate.Length <= 128)
				{
					return candidate;
				}
			}

			return Guid.NewGuid().ToString();
		}
	}

	public static class CorrelationIdMiddlewareExtensions
	{
		public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
			=> app.UseMiddleware<CorrelationIdMiddleware>();
	}
}
