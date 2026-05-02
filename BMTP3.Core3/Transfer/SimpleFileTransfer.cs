using Microsoft.Extensions.Logging;

namespace BMTP3.Core3.Transfer;

/// <summary>
/// Simple file transfer implementation.
/// Copies files with progress reporting and collision handling.
/// </summary>
public class SimpleFileTransfer : IFileTransfer
{
	private const int BufferSize = 81920;  // 80 KB
	private readonly ILogger<SimpleFileTransfer> _logger;

	public SimpleFileTransfer(ILogger<SimpleFileTransfer> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async Task CopyAsync(BackupItem item, string destinationDirectory, IProgress<long>? progress, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(item);
		ArgumentNullException.ThrowIfNullOrWhiteSpace(destinationDirectory);
		ct.ThrowIfCancellationRequested();

		if (!File.Exists(item.SourcePath))
		{
			throw new FileNotFoundException($"Source file not found: {item.SourcePath}");
		}

		if (!Directory.Exists(destinationDirectory))
		{
			throw new DirectoryNotFoundException($"Destination directory not found: {destinationDirectory}");
		}

		var destPath = ResolveDestinationPath(destinationDirectory, item.Name);

		_logger.LogDebug("Copying {SourcePath} → {DestPath}", item.SourcePath, destPath);

		try
		{
			using var sourceStream = new FileStream(item.SourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true);
			using var destStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);

			var buffer = new byte[BufferSize];
			int bytesRead;
			long totalBytesRead = 0;

			while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false)) > 0)
			{
				await destStream.WriteAsync(buffer, 0, bytesRead, ct).ConfigureAwait(false);
				totalBytesRead += bytesRead;
				progress?.Report(totalBytesRead);
			}

			_logger.LogDebug("Transfer complete: {SourcePath} ({BytesCopied} bytes)", item.SourcePath, totalBytesRead);
		}
		catch (OperationCanceledException)
		{
			_logger.LogWarning("Transfer cancelled: {SourcePath}", item.SourcePath);
			if (File.Exists(destPath))
			{
				File.Delete(destPath);  // Clean up incomplete file
			}
			throw;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Transfer failed: {SourcePath}", item.SourcePath);
			if (File.Exists(destPath))
			{
				File.Delete(destPath);  // Clean up incomplete file
			}
			throw;
		}
	}

	/// <summary>
	/// Resolve the destination path, handling collisions by renaming.
	/// If file exists, renames to filename_1.ext, filename_2.ext, etc.
	/// </summary>
	private static string ResolveDestinationPath(string destinationDirectory, string fileName)
	{
		var destPath = Path.Combine(destinationDirectory, fileName);

		// If file doesn't exist, use as-is
		if (!File.Exists(destPath))
		{
			return destPath;
		}

		// File exists, need to rename with _N suffix
		var name = Path.GetFileNameWithoutExtension(fileName);
		var ext = Path.GetExtension(fileName);

		for (int i = 1; i <= 10000; i++)
		{
			var newName = $"{name}_{i}{ext}";
			var newPath = Path.Combine(destinationDirectory, newName);

			if (!File.Exists(newPath))
			{
				return newPath;
			}
		}

		throw new InvalidOperationException($"Cannot resolve destination path for {fileName}: too many collisions");
	}
}
