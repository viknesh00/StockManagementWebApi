namespace StockManagementWebApi.Common.Exceptions
{
	/// <summary>
	/// Base type for every exception the application throws deliberately.
	/// Anything deriving from this is considered "expected" - the global exception
	/// middleware trusts <see cref="Message"/> and <see cref="Errors"/> to be safe to
	/// return to the API consumer in any environment.
	/// </summary>
	public abstract class AppException : Exception
	{
		protected AppException(int statusCode, string message, IEnumerable<string>? errors = null, Exception? innerException = null)
			: base(message, innerException)
		{
			StatusCode = statusCode;
			Errors = errors?.Where(e => !string.IsNullOrWhiteSpace(e)).ToList() ?? new List<string>();
		}

		/// <summary>HTTP status code this exception maps to.</summary>
		public int StatusCode { get; }

		/// <summary>Zero or more consumer-safe detail messages.</summary>
		public IReadOnlyList<string> Errors { get; }
	}
}
