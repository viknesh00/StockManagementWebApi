using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace StockManagementWebApi.Common.Exceptions
{
	/// <summary>The consumer-facing shape of a translated exception.</summary>
	public readonly struct TranslatedException
	{
		public TranslatedException(int statusCode, string message, IReadOnlyList<string> errors, LogLevel logLevel)
		{
			StatusCode = statusCode;
			Message = message;
			Errors = errors;
			LogLevel = logLevel;
		}

		public int StatusCode { get; }

		public string Message { get; }

		public IReadOnlyList<string> Errors { get; }

		public LogLevel LogLevel { get; }
	}

	/// <summary>
	/// Turns any exception into a status code plus a message that is safe to hand to an API
	/// consumer. Database and infrastructure details are deliberately collapsed into generic
	/// wording here - the full exception is still logged in full by the caller.
	/// </summary>
	public static class ExceptionTranslator
	{
		private static readonly IReadOnlyList<string> NoErrors = Array.Empty<string>();

		public static TranslatedException Translate(Exception exception)
		{
			switch (exception)
			{
				// Exceptions the application raised on purpose: message and errors are already safe.
				case AppException appException:
					return new TranslatedException(
						appException.StatusCode,
						appException.Message,
						appException.Errors,
						appException.StatusCode >= StatusCodes.Status500InternalServerError ? LogLevel.Error : LogLevel.Warning);

				case DbUpdateConcurrencyException:
					return new TranslatedException(
						StatusCodes.Status409Conflict,
						"The record was modified by another user. Please reload and try again.",
						NoErrors,
						LogLevel.Warning);

				case DbUpdateException dbUpdateException:
					return dbUpdateException.GetBaseException() is SqlException innerSql
						? TranslateSqlException(innerSql)
						: new TranslatedException(
							StatusCodes.Status409Conflict,
							"The changes could not be saved because they conflict with existing data.",
							NoErrors,
							LogLevel.Error);

				case SqlException sqlException:
					return TranslateSqlException(sqlException);

				case TimeoutException:
					return new TranslatedException(
						StatusCodes.Status504GatewayTimeout,
						"The request timed out. Please try again.",
						NoErrors,
						LogLevel.Error);

				case UnauthorizedAccessException:
					// Thrown by both the file system and ASP.NET; treat as an authorization failure.
					return new TranslatedException(
						StatusCodes.Status403Forbidden,
						"You do not have permission to perform this action.",
						NoErrors,
						LogLevel.Warning);

				case BadHttpRequestException badHttpRequest:
					return new TranslatedException(
						StatusCodes.Status400BadRequest,
						"The request could not be read.",
						new[] { badHttpRequest.Message },
						LogLevel.Warning);

				case FormatException:
					return new TranslatedException(
						StatusCodes.Status400BadRequest,
						"One or more request values are not in the expected format.",
						NoErrors,
						LogLevel.Warning);

				// ArgumentException and friends are deliberately *not* mapped to 400: reaching
				// one means the server passed bad arguments to its own code, which is a defect,
				// not something the caller can correct.
				case NotSupportedException:
				case InvalidOperationException:
					// Typically a sequence/state bug rather than anything the caller can fix.
					return new TranslatedException(
						StatusCodes.Status500InternalServerError,
						"The request could not be completed due to an unexpected server state.",
						NoErrors,
						LogLevel.Error);

				case IOException:
					return new TranslatedException(
						StatusCodes.Status500InternalServerError,
						"A file operation failed while processing the request.",
						NoErrors,
						LogLevel.Error);

				default:
					return new TranslatedException(
						StatusCodes.Status500InternalServerError,
						"An unexpected error occurred while processing your request.",
						NoErrors,
						LogLevel.Error);
			}
		}

		/// <summary>
		/// Maps SQL Server error numbers onto meaningful responses. Engine-level messages are
		/// replaced with generic wording; messages raised by the application's own stored
		/// procedures (RAISERROR uses numbers from 50000 up) are passed through, since those are
		/// written for end users.
		/// </summary>
		private static TranslatedException TranslateSqlException(SqlException sqlException)
		{
			switch (sqlException.Number)
			{
				case 2627: // Unique constraint violation.
				case 2601: // Duplicate key row in a unique index.
					return new TranslatedException(
						StatusCodes.Status409Conflict,
						"A record with the same key already exists.",
						NoErrors,
						LogLevel.Warning);

				case 547: // Foreign key / check constraint violation.
					return new TranslatedException(
						StatusCodes.Status409Conflict,
						"The operation conflicts with related records and cannot be completed.",
						NoErrors,
						LogLevel.Warning);

				case 515: // Cannot insert NULL into a non-nullable column.
					return new TranslatedException(
						StatusCodes.Status400BadRequest,
						"A required value was missing from the request.",
						NoErrors,
						LogLevel.Warning);

				case 245: // Conversion failed.
				case 8114:
					return new TranslatedException(
						StatusCodes.Status400BadRequest,
						"One or more request values are not in the expected format.",
						NoErrors,
						LogLevel.Warning);

				case 2628: // String or binary data would be truncated (with column detail).
				case 8152:
					return new TranslatedException(
						StatusCodes.Status400BadRequest,
						"One or more values exceed the maximum allowed length.",
						NoErrors,
						LogLevel.Warning);

				case -2: // Command timeout.
				case 1222: // Lock request timeout.
					return new TranslatedException(
						StatusCodes.Status504GatewayTimeout,
						"The database did not respond in time. Please try again.",
						NoErrors,
						LogLevel.Error);

				case 1205: // Deadlock victim.
					return new TranslatedException(
						StatusCodes.Status409Conflict,
						"The request could not be completed because of a conflicting operation. Please try again.",
						NoErrors,
						LogLevel.Warning);

				case 4060: // Cannot open database.
				case 18456: // Login failed.
				case 40613: // Azure SQL database unavailable.
				case 40197:
				case 40501:
				case 49918:
				case 49919:
				case 49920:
					return new TranslatedException(
						StatusCodes.Status503ServiceUnavailable,
						"The service is temporarily unavailable. Please try again shortly.",
						NoErrors,
						LogLevel.Critical);

				default:
					// RAISERROR / THROW from the application's own stored procedures.
					if (sqlException.Number >= 50000)
					{
						return new TranslatedException(
							StatusCodes.Status422UnprocessableEntity,
							sqlException.Message,
							NoErrors,
							LogLevel.Warning);
					}

					return new TranslatedException(
						StatusCodes.Status500InternalServerError,
						"A database error occurred while processing your request.",
						NoErrors,
						LogLevel.Error);
			}
		}
	}
}
