using Microsoft.AspNetCore.Http;

namespace StockManagementWebApi.Common.Exceptions
{
	/// <summary>The requested resource does not exist. Maps to HTTP 404.</summary>
	public class NotFoundException : AppException
	{
		public NotFoundException(string message = "The requested resource was not found.", IEnumerable<string>? errors = null, Exception? innerException = null)
			: base(StatusCodes.Status404NotFound, message, errors, innerException)
		{
		}

		/// <summary>Convenience form producing "Company 'ABC' was not found."</summary>
		public static NotFoundException For(string resource, object key)
			=> new NotFoundException($"{resource} '{key}' was not found.");
	}
}
