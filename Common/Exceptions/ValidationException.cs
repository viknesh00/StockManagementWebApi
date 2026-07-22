using Microsoft.AspNetCore.Http;

namespace StockManagementWebApi.Common.Exceptions
{
	/// <summary>
	/// One or more input values failed validation. Maps to HTTP 400 and always carries a
	/// structured list of per-field messages in <see cref="AppException.Errors"/>.
	/// </summary>
	public class ValidationException : AppException
	{
		public ValidationException(IEnumerable<string> errors, string message = "Validation failed.", Exception? innerException = null)
			: base(StatusCodes.Status400BadRequest, message, errors, innerException)
		{
		}

		public ValidationException(string error, string message = "Validation failed.")
			: this(new[] { error }, message)
		{
		}

		/// <summary>Builds a validation exception from a field -> messages map.</summary>
		public ValidationException(IDictionary<string, string[]> fieldErrors, string message = "Validation failed.")
			: this(Flatten(fieldErrors), message)
		{
		}

		private static IEnumerable<string> Flatten(IDictionary<string, string[]> fieldErrors)
		{
			return fieldErrors
				.SelectMany(pair => pair.Value.Select(msg =>
					string.IsNullOrWhiteSpace(pair.Key) ? msg : $"{pair.Key}: {msg}"));
		}
	}
}
