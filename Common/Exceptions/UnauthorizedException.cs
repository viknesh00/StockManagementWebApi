using Microsoft.AspNetCore.Http;

namespace StockManagementWebApi.Common.Exceptions
{
	/// <summary>
	/// The caller could not be authenticated - bad credentials, or a missing, expired or
	/// malformed token. Maps to HTTP 401.
	/// </summary>
	public class UnauthorizedException : AppException
	{
		public UnauthorizedException(string message = "Authentication is required to access this resource.", IEnumerable<string>? errors = null, Exception? innerException = null)
			: base(StatusCodes.Status401Unauthorized, message, errors, innerException)
		{
		}
	}
}
