using Microsoft.AspNetCore.Http;

namespace StockManagementWebApi.Common.Exceptions
{
	/// <summary>
	/// A domain rule rejected an otherwise well-formed request - for example outwarding a
	/// serial number that has already been outwarded, or delivering more stock than is on
	/// hand. Maps to HTTP 422 Unprocessable Entity by default.
	/// </summary>
	public class BusinessException : AppException
	{
		public BusinessException(string message, IEnumerable<string>? errors = null, Exception? innerException = null)
			: base(StatusCodes.Status422UnprocessableEntity, message, errors, innerException)
		{
		}

		/// <summary>
		/// For rules that must keep reporting a different status code than 422 so existing
		/// clients are not broken.
		/// </summary>
		public BusinessException(int statusCode, string message, IEnumerable<string>? errors = null, Exception? innerException = null)
			: base(statusCode, message, errors, innerException)
		{
		}
	}
}
