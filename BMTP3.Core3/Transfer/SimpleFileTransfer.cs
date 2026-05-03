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

	public async Task<string> CopyAsync(
		BackupItem item,
		string destinationDirectory,
		CollisionStrategy collisionStrategy,
		IProgress<long>? progress,
		CancellationToken ct
	)
	{
		ArgumentNullException.ThrowIfNull(item);
		ArgumentNullException.ThrowIfNullOrWhiteSpace(destinationDirectory);
		ct.ThrowIfCancellationRequested();

		if(!File.Exists(item.SourcePath))
		{
			throw new FileNotFoundException($"Source file not found: {item.SourcePath}");
		}

		if(!Directory.Exists(destinationDirectory))
		{
			throw new DirectoryNotFoundException($"Destination directory not found: {destinationDirectory}");
		}

		string destPath = ResolveDestinationPath(destinationDirectory, item.Name, collisionStrategy);
		bool isOverwrite = collisionStrategy == CollisionStrategy.Overwrite && File.Exists(destPath);

		if(isOverwrite)
		{
			_logger.LogInformation("Overwriting existing file: {DestPath}", destPath);
		}

		_logger.LogDebug("Copying {SourcePath} → {DestPath}", item.SourcePath, destPath);

		try
		{
			FileMode destinationFileMode = collisionStrategy == CollisionStrategy.Overwrite
				? FileMode.Create
				: FileMode.CreateNew;

			using FileStream sourceStream = new(item.SourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true);
			using FileStream destStream = new(destPath, destinationFileMode, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);

			byte[] buffer = new byte[BufferSize];
			int bytesRead;
			long totalBytesRead = 0;

			while((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false)) > 0)
			{
				await destStream.WriteAsync(buffer, 0, bytesRead, ct).ConfigureAwait(false);
				totalBytesRead += bytesRead;
				progress?.Report(totalBytesRead);
			}

			_logger.LogDebug("Transfer complete: {SourcePath} ({BytesCopied} bytes)", item.SourcePath, totalBytesRead);
			return destPath;
		} catch(OperationCanceledException)
		{
			_logger.LogWarning("Transfer cancelled: {SourcePath}", item.SourcePath);
			if(File.Exists(destPath))
			{
				File.Delete(destPath);  // Clean up incomplete file
			}
			throw;
		} catch(Exception ex)
		{
			_logger.LogError(ex, "Transfer failed: {SourcePath}", item.SourcePath);
			if(File.Exists(destPath))
			{
				File.Delete(destPath);  // Clean up incomplete file
			}
			throw;
		}
	}

	/// <summary>
	/// Resolve destination path based on collision strategy.
	/// </summary>
	private static string ResolveDestinationPath(string destinationDirectory, string fileName, CollisionStrategy collisionStrategy)
	{
		string destPath = Path.Combine(destinationDirectory, fileName);

		if(!File.Exists(destPath))
		{
			return destPath;
		}

		switch(collisionStrategy)
		{
			case CollisionStrategy.Overwrite:
				return destPath;

			case CollisionStrategy.Rename:
				string name = Path.GetFileNameWithoutExtension(fileName);
				string ext = Path.GetExtension(fileName);

				for(int i = 1; i <= 10000; i++)
				{
					string newName = $"{name}_{i}{ext}";
					string newPath = Path.Combine(destinationDirectory, newName);

					if(!File.Exists(newPath))
					{
						return newPath;
					}
				}

				throw new InvalidOperationException($"Cannot resolve destination path for {fileName}: too many collisions");

			case CollisionStrategy.Skip:
				throw new IOException($"Destination file already exists and strategy is Skip: {destPath}");

			case CollisionStrategy.Error:
				throw new IOException($"Destination file already exists and strategy is Error: {destPath}");

			default:
				throw new ArgumentOutOfRangeException(nameof(collisionStrategy), collisionStrategy, "Unknown collision strategy");
		}
	}
}
