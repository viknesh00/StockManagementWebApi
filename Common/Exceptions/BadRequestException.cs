using Microsoft.AspNetCore.Http;

namespace StockManagementWebApi.Common.Exceptions
{
	/// <summary>The request itself is malformed or missing required input. Maps to HTTP 400.</summary>
	public class BadRequestException : AppException
	{
		public BadRequestException(string message = "The request is invalid.", IEnumerable<string>? errors = null, Exception? innerException = null)
			: base(StatusCodes.Status400BadRequest, message, errors, innerException)
		{
		}
	}
}
