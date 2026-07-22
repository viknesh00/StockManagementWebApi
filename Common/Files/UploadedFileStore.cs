using StockManagementWebApi.Common.Exceptions;

namespace StockManagementWebApi.Common.Files
{
	/// <summary>
	/// Stages an uploaded file on disk for the lifetime of a single request and guarantees it is
	/// removed afterwards. Used by the Excel import endpoints, which need a real file path
	/// because EPPlus reads from <see cref="FileInfo"/>.
	/// </summary>
	public interface IUploadedFileStore
	{
		/// <summary>
		/// Writes <paramref name="file"/> to the uploads directory under a generated name and
		/// returns the full path. Throws <see cref="BadRequestException"/> when nothing was sent.
		/// </summary>
		Task<string> SaveAsync(IFormFile? file, CancellationToken cancellationToken = default);

		/// <summary>Deletes a staged file. Never throws - failures are logged and swallowed.</summary>
		void TryDelete(string? path);
	}

	public class UploadedFileStore : IUploadedFileStore
	{
		private readonly IWebHostEnvironment _environment;
		private readonly ILogger<UploadedFileStore> _logger;

		public UploadedFileStore(IWebHostEnvironment environment, ILogger<UploadedFileStore> logger)
		{
			_environment = environment;
			_logger = logger;
		}

		public async Task<string> SaveAsync(IFormFile? file, CancellationToken cancellationToken = default)
		{
			if (file == null || file.Length == 0)
			{
				throw new BadRequestException("No file uploaded.");
			}

			var uploadsDirectory = Path.Combine(_environment.ContentRootPath, "Uploads");
			Directory.CreateDirectory(uploadsDirectory);

			// A generated name rather than the client-supplied one: the original name is
			// attacker-controlled (path traversal) and collides when two imports overlap.
			var extension = Path.GetExtension(file.FileName);
			var filePath = Path.Combine(uploadsDirectory, $"{Guid.NewGuid():N}{extension}");

			try
			{
				await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
				await file.CopyToAsync(stream, cancellationToken);
			}
			catch (Exception exception)
			{
				TryDelete(filePath);
				_logger.LogError(exception, "Failed to stage uploaded file {FileName} at {FilePath}.", file.FileName, filePath);
				throw;
			}

			return filePath;
		}

		public void TryDelete(string? path)
		{
			if (string.IsNullOrEmpty(path))
			{
				return;
			}

			try
			{
				if (File.Exists(path))
				{
					File.Delete(path);
				}
			}
			catch (Exception exception)
			{
				// Cleanup failure must never turn a successful request into a failed one.
				_logger.LogWarning(exception, "Could not delete staged upload {FilePath}.", path);
			}
		}
	}
}
