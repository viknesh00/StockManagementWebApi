using Microsoft.EntityFrameworkCore;
using StockManagementWebApi.Models;

namespace StockManagementWebApi.Common.Auth
{
	/// <summary>
	/// Startup check that the refresh-token table exists.
	/// </summary>
	/// <remarks>
	/// Without it the first sign-in fails with "Invalid object name 'sm_RefreshTokens'" as an
	/// opaque 500, which looks like an application bug rather than a missed deployment step.
	/// This turns it into one loud line in the startup log.
	/// </remarks>
	public static class AuthSchemaCheck
	{
		private const string ScriptPath = "Database/Scripts/001_CreateRefreshTokens.sql";

		public static async Task VerifyRefreshTokenStoreAsync(IServiceProvider services, ILogger logger, CancellationToken cancellationToken = default)
		{
			try
			{
				await using var scope = services.CreateAsyncScope();
				var context = scope.ServiceProvider.GetRequiredService<MydbContext>();

				var exists = await context.Database
					.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM sys.tables WHERE name = 'sm_RefreshTokens'")
					.FirstOrDefaultAsync(cancellationToken);

				if (exists > 0)
				{
					logger.LogInformation("Refresh-token store is present; authentication is ready.");
					return;
				}

				logger.LogCritical(
					"The table sm_RefreshTokens does not exist. Sign-in and token refresh WILL FAIL " +
					"until {ScriptPath} has been run against this database.",
					ScriptPath);
			}
			catch (Exception exception)
			{
				// A database that is merely unreachable at boot must not stop the API from
				// starting - it may come back. Log it and carry on.
				logger.LogError(
					exception,
					"Could not verify the refresh-token store at startup. If sign-in fails, check that {ScriptPath} has been run.",
					ScriptPath);
			}
		}
	}
}
