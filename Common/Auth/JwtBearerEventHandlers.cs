using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using StockManagementWebApi.Common.Models;

namespace StockManagementWebApi.Common.Auth
{
	/// <summary>
	/// Turns JWT bearer authentication failures into the standard envelope.
	/// </summary>
	/// <remarks>
	/// The bearer middleware never throws into the request pipeline - it handles token problems
	/// internally and issues a bare challenge. Without these handlers a missing or expired token
	/// would produce an empty 401 with a WWW-Authenticate header, which is the one response
	/// shape the rest of the API does not use.
	/// </remarks>
	public static class JwtBearerEventHandlers
	{
		/// <summary>
		/// Response header set when the token was well-formed but past its expiry. The client
		/// uses it to tell "refresh me" apart from "sign in again".
		/// </summary>
		public const string TokenExpiredHeader = "Token-Expired";

		/// <summary>
		/// Key under which the "token was expired" flag is carried from
		/// <c>OnAuthenticationFailed</c> to <c>OnChallenge</c>. Kept in
		/// <see cref="HttpContext.Items"/> rather than on the response, because writing the
		/// envelope resets the response headers.
		/// </summary>
		private const string TokenExpiredFlag = "__jwt_token_expired";

		public static JwtBearerEvents Create() => new JwtBearerEvents
		{
			OnAuthenticationFailed = context =>
			{
				var logger = GetLogger(context.HttpContext);

				if (context.Exception is SecurityTokenExpiredException expired)
				{
					// Flag it so the client refreshes instead of bouncing the user to login.
					context.HttpContext.Items[TokenExpiredFlag] = true;

					logger.LogInformation(
						"Access token expired at {ExpiredAt} for {Method} {Path}. CorrelationId: {CorrelationId}",
						expired.Expires, context.Request.Method, context.Request.Path, context.HttpContext.TraceIdentifier);
				}
				else
				{
					logger.LogWarning(
						context.Exception,
						"Token validation failed ({ExceptionType}) for {Method} {Path}. CorrelationId: {CorrelationId}",
						context.Exception.GetType().Name, context.Request.Method, context.Request.Path,
						context.HttpContext.TraceIdentifier);
				}

				return Task.CompletedTask;
			},

			OnChallenge = async context =>
			{
				// Suppress the default empty-body challenge and write our envelope instead.
				context.HandleResponse();

				var isExpired = context.HttpContext.Items.ContainsKey(TokenExpiredFlag);

				var message = isExpired
					? "Your session has expired. Please refresh your session or sign in again."
					: "Authentication is required to access this resource.";

				await ApiResponseWriter.WriteErrorAsync(
					context.HttpContext,
					StatusCodes.Status401Unauthorized,
					message,
					configureResponse: response =>
					{
						if (isExpired)
						{
							response.Headers[TokenExpiredHeader] = "true";
						}
					});
			},

			OnForbidden = async context =>
			{
				GetLogger(context.HttpContext).LogWarning(
					"Authenticated user {User} was forbidden from {Method} {Path}. CorrelationId: {CorrelationId}",
					context.HttpContext.User?.Identity?.Name ?? "unknown",
					context.Request.Method, context.Request.Path, context.HttpContext.TraceIdentifier);

				await ApiResponseWriter.WriteErrorAsync(
					context.HttpContext,
					StatusCodes.Status403Forbidden,
					"You do not have permission to perform this action.");
			}
		};

		private static ILogger GetLogger(HttpContext context) =>
			context.RequestServices
				.GetRequiredService<ILoggerFactory>()
				.CreateLogger("StockManagementWebApi.Authentication");
	}
}
