using Microsoft.AspNetCore.Http;

namespace StockManagementWebApi.Common.Exceptions
{
	/// <summary>
	/// The caller is authenticated but not allowed to perform this action. Maps to HTTP 403.
	/// </summary>
	public class ForbiddenException : AppException
	{
		public ForbiddenException(string message = "You do not have permission to perform this action.", IEnumerable<string>? errors = null, Exception? innerException = null)
			: base(StatusCodes.Status403Forbidden, message, errors, innerException)
		{
		}
	}
}
