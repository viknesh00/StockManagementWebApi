using Microsoft.Data.SqlClient;

namespace StockManagementWebApi.Common.Exceptions
{
	/// <summary>SQL Server error numbers this application reacts to by name rather than by digit.</summary>
	public static class SqlErrorCodes
	{
		/// <summary>Violation of a UNIQUE constraint.</summary>
		public const int UniqueConstraintViolation = 2627;

		/// <summary>Duplicate key row in an object with a unique index.</summary>
		public const int DuplicateKeyInUniqueIndex = 2601;

		/// <summary>Custom RAISERROR number the stock stored procedures use for duplicate keys.</summary>
		public const int ApplicationDuplicateKey = 50001;

		/// <summary>True when the exception reports an attempt to insert a row that already exists.</summary>
		public static bool IsDuplicateKey(this SqlException exception)
			=> exception.Number is UniqueConstraintViolation
				or DuplicateKeyInUniqueIndex
				or ApplicationDuplicateKey;

		/// <summary>
		/// True when the message came from the application's own RAISERROR / THROW rather than
		/// from the database engine, and is therefore safe to show to the caller.
		/// </summary>
		public static bool IsApplicationRaised(this SqlException exception)
			=> exception.Number >= 50000;
	}
}
