using Microsoft.AspNetCore.Http;

namespace StockManagementWebApi.Common.Exceptions
{
	/// <summary>
	/// The request conflicts with the current state of the resource - duplicate keys,
	/// concurrent edits, referential integrity violations. Maps to HTTP 409.
	/// </summary>
	public class ConflictException : AppException
	{
		public ConflictException(string message = "The request conflicts with the current state of the resource.", IEnumerable<string>? errors = null, Exception? innerException = null)
			: base(StatusCodes.Status409Conflict, message, errors, innerException)
		{
		}
	}
}
