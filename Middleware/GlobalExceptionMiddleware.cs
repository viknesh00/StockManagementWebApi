using System.Security.Claims;
using System.Text.Json;
using StockManagementWebApi.Common.Exceptions;
using StockManagementWebApi.Common.Models;

namespace StockManagementWebApi.Middleware
{
	/// <summary>
	/// Last line of defence for the request pipeline. Every exception that escapes a controller
	/// lands here, is logged with full context, and is turned into the standard
	/// <see cref="ApiResponse"/> envelope. Nothing internal - stack traces, SQL text, connection
	/// details - ever reaches the caller outside the Development environment.
	/// </summary>
	public class GlobalExceptionMiddleware
	{
		private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

		private readonly RequestDelegate _next;
		private readonly ILogger<GlobalExceptionMiddleware> _logger;
		private readonly IHostEnvironment _environment;

		public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment environment)
		{
			_next = next;
			_logger = logger;
			_environment = environment;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			try
			{
				await _next(context);
			}
			catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
			{
				// The client went away mid-request. Not a fault, and there is nobody left to
				// answer, so log it quietly and let the connection close.
				_logger.LogInformation(
					"Request {Method} {Path} was cancelled by the client. CorrelationId: {CorrelationId}",
					context.Request.Method, context.Request.Path, context.TraceIdentifier);

				if (!context.Response.HasStarted)
				{
					context.Response.StatusCode = 499; // Nginx's "client closed request".
				}
			}
			catch (Exception exception)
			{
				await HandleExceptionAsync(context, exception);
			}
		}

		private async Task HandleExceptionAsync(HttpContext context, Exception exception)
		{
			var translated = ExceptionTranslator.Translate(exception);
			var correlationId = context.TraceIdentifier;

			Log(context, exception, translated, correlationId);

			if (context.Response.HasStarted)
			{
				// Headers are already on the wire; we cannot replace the body with an envelope.
				// Surfacing this as Critical because the caller will receive a truncated payload.
				_logger.LogCritical(
					exception,
					"Response had already started when {ExceptionType} was caught; the error envelope could not be written. CorrelationId: {CorrelationId}",
					exception.GetType().FullName, correlationId);
				return;
			}

			context.Response.Clear();
			context.Response.StatusCode = translated.StatusCode;
			context.Response.ContentType = "application/json; charset=utf-8";

			var payload = ApiResponse.Fail(translated.Message, translated.StatusCode, correlationId, translated.Errors);

			if (_environment.IsDevelopment())
			{
				payload.Developer = new DeveloperDetail
				{
					ExceptionType = exception.GetType().FullName ?? exception.GetType().Name,
					ExceptionMessage = exception.Message,
					InnerExceptionMessage = exception.InnerException?.Message,
					StackTrace = exception.StackTrace
				};
			}

			await context.Response.WriteAsync(JsonSerializer.Serialize(payload, SerializerOptions), context.RequestAborted);
		}

		private void Log(HttpContext context, Exception exception, TranslatedException translated, string correlationId)
		{
			const string template =
				"Unhandled {ExceptionType} handling {Method} {Path} -> {StatusCode}. " +
				"User: {UserId}. Endpoint: {Endpoint}. CorrelationId: {CorrelationId}. Detail: {ExceptionMessage}";

			var args = new object?[]
			{
				exception.GetType().FullName,
				context.Request.Method,
				context.Request.Path.ToString(),
				translated.StatusCode,
				ResolveUserId(context),
				context.GetEndpoint()?.DisplayName ?? "(unmatched)",
				correlationId,
				exception.Message
			};

			// The exception object is always passed to the logger, so the stack trace reaches the
			// log sink in every environment - it just never reaches the HTTP response.
			switch (translated.LogLevel)
			{
				case LogLevel.Critical:
					_logger.LogCritical(exception, template, args);
					break;
				case LogLevel.Warning:
					_logger.LogWarning(exception, template, args);
					break;
				default:
					_logger.LogError(exception, template, args);
					break;
			}
		}

		private static string ResolveUserId(HttpContext context)
		{
			var user = context.User;
			if (user?.Identity?.IsAuthenticated != true)
			{
				return "anonymous";
			}

			return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
				?? user.FindFirst("sub")?.Value
				?? user.Identity.Name
				?? "authenticated";
		}
	}

	public static class GlobalExceptionMiddlewareExtensions
	{
		public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
			=> app.UseMiddleware<GlobalExceptionMiddleware>();
	}
}
