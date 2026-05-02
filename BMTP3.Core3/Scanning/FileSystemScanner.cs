using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace BMTP3.Core3.Scanning;

/// <summary>
/// Filesystem scanner: recursively traverses directories and yields BackupItem for each file.
/// </summary>
public class FileSystemScanner : IBackupScanner
{
	private readonly ILogger<FileSystemScanner> _logger;

	public FileSystemScanner(ILogger<FileSystemScanner> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async Task<IEnumerable<BackupItem>> ScanAsync(string source, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNullOrWhiteSpace(source);
		ct.ThrowIfCancellationRequested();

		var items = new List<BackupItem>();

		if (!Directory.Exists(source))
		{
			throw new FileNotFoundException($"Source directory not found: {source}");
		}

		_logger.LogInformation("Starting filesystem scan of {Source}", source);

		try
		{
			await ScanDirectoryRecursiveAsync(source, items, ct);
			_logger.LogInformation("Scan complete: found {ItemCount} items", items.Count);
			return items;
		}
		catch (OperationCanceledException)
		{
			_logger.LogWarning("Scan cancelled after {ItemCount} items", items.Count);
			throw;
		}
		catch (Exception)
		{
			_logger.LogError("Scan failed after {ItemCount} items", items.Count);
			throw;
		}
	}

	private async Task ScanDirectoryRecursiveAsync(string directoryPath, List<BackupItem> items, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

		var dirInfo = new DirectoryInfo(directoryPath);

		// Scan files in this directory
		foreach (var fileInfo in dirInfo.GetFiles())
		{
			ct.ThrowIfCancellationRequested();

			items.Add(new BackupItem
			{
				Name = fileInfo.Name,
				SourcePath = fileInfo.FullName,
				DestinationPath = fileInfo.Name,  // Will be updated by collision resolution
				Type = BackupItemType.File,
				SizeInBytes = fileInfo.Length,
				CreatedAt = fileInfo.CreationTime,
				ModifiedAt = fileInfo.LastWriteTime
			});
		}

		// Recursively scan subdirectories
		foreach (var subdirInfo in dirInfo.GetDirectories())
		{
			ct.ThrowIfCancellationRequested();

			await ScanDirectoryRecursiveAsync(subdirInfo.FullName, items, ct);
		}
	}
}
