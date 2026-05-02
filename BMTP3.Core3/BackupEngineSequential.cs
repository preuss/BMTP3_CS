using System.Diagnostics;
using Microsoft.Extensions.Logging;
using BMTP3.Core3.Scanning;
using BMTP3.Core3.Transfer;
using BMTP3.Core3.Hashing;
using BMTP3.Core3.Metadata;
using BMTP3.Core3.Sidecar;

namespace BMTP3.Core3;

/// <summary>
/// Sequential backup engine orchestrator - single-threaded implementation.
/// Immutable data flow pipeline:
/// Scan → Transfer+Sidecar → ExtractMetadata → GenerateHashes → CorrectTimestamps → Result
/// </summary>
public class BackupEngineSequential : IBackupEngine
{
	private readonly IBackupScanner _scanner;
	private readonly IFileTransfer _fileTransfer;
	private readonly IItemHasher _itemHasher;
	private readonly IMetadataReader _metadataReader;
	private readonly ISidecarGenerator _sidecarGenerator;
	private readonly ILogger<BackupEngineSequential> _logger;

	public BackupEngineSequential(
		IBackupScanner scanner,
		IFileTransfer fileTransfer,
		IItemHasher itemHasher,
		IMetadataReader metadataReader,
		ISidecarGenerator sidecarGenerator,
		ILogger<BackupEngineSequential> logger)
	{
		_scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
		_fileTransfer = fileTransfer ?? throw new ArgumentNullException(nameof(fileTransfer));
		_itemHasher = itemHasher ?? throw new ArgumentNullException(nameof(itemHasher));
		_metadataReader = metadataReader ?? throw new ArgumentNullException(nameof(metadataReader));
		_sidecarGenerator = sidecarGenerator ?? throw new ArgumentNullException(nameof(sidecarGenerator));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async Task<BackupJobResult> RunAsync(
		BackupPlan plan,
		IProgress<IBackupProgress>? progress,
		CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(plan);
		ct.ThrowIfCancellationRequested();

		var stopwatch = Stopwatch.StartNew();
		var errors = new List<BackupError>();
		var items = new List<BackupItem>();

		try
		{
			_logger.LogInformation("Starting backup: {Source} → {Destination}", plan.Source, plan.Destination);

			// Step 1: Scan
			items = await Scan(plan, progress, ct);

			// Step 2: Transfer + Sidecar (per-file operation)
			items = await TransferAndGenerateSidecars(items, plan, progress, ct);

			// Step 3: Extract Metadata (non-critical failures)
			items = await ExtractMetadata(items, progress, ct);

			// Step 4: Generate Hashes (non-critical failures)
			items = await GenerateHashes(items, plan, progress, ct);

			// Step 5: Correct Timestamps (non-critical failures)
			items = await CorrectTimestamps(items, progress, ct);

			stopwatch.Stop();

			_logger.LogInformation("Backup complete: {ItemCount} items in {Duration}ms", items.Count, stopwatch.ElapsedMilliseconds);

			return BuildSuccessResult(items, stopwatch.Elapsed);
		}
		catch (OperationCanceledException ex)
		{
			stopwatch.Stop();
			_logger.LogWarning("Backup cancelled after {Duration}ms", stopwatch.ElapsedMilliseconds);
			errors.Add(new BackupError { ItemName = "Backup", Message = "Operation cancelled", Exception = ex });
			return BuildFailureResult(items, errors, stopwatch.Elapsed);
		}
		catch (Exception ex)
		{
			stopwatch.Stop();
			_logger.LogError(ex, "Backup failed after {Duration}ms", stopwatch.ElapsedMilliseconds);
			errors.Add(new BackupError { ItemName = "Backup", Message = ex.Message, Exception = ex });
			return BuildFailureResult(items, errors, stopwatch.Elapsed);
		}
	}

	private async Task<List<BackupItem>> Scan(BackupPlan plan, IProgress<IBackupProgress>? progress, CancellationToken ct)
	{
		_logger.LogInformation("Phase: Scanning");
		progress?.Report(new BackupProgress { Phase = BackupPhase.Scanning, CurrentFile = plan.Source, FilesTotal = 0 });

		var scannedItems = await _scanner.ScanAsync(plan.Source, ct);
		var items = scannedItems.ToList();

		_logger.LogInformation("Scan complete: {ItemCount} items found", items.Count);
		return items;
	}

	private async Task<List<BackupItem>> TransferAndGenerateSidecars(
		List<BackupItem> items,
		BackupPlan plan,
		IProgress<IBackupProgress>? progress,
		CancellationToken ct)
	{
		_logger.LogInformation("Phase: Transfer (with sidecars)");

		var results = new List<BackupItem>();

		for (int i = 0; i < items.Count; i++)
		{
			var item = items[i];

			progress?.Report(new BackupProgress
			{
				Phase = BackupPhase.Transferring,
				CurrentFile = item.Name,
				FilesProcessed = i,
				FilesTotal = items.Count
			});

			try
			{
				// Transfer file (critical: fail if this fails)
				if (!plan.DryRun)
				{
					await _fileTransfer.CopyAsync(item, plan.Destination, null, ct);
				}

				// Update item with destination path (may have been renamed due to collision)
				var destPath = Path.Combine(plan.Destination, item.Name);
				item = item.WithDestinationPath(destPath);

				// Generate sidecar immediately (non-critical: tolerate failure)
				try
				{
					await _sidecarGenerator.GenerateAsync(item, plan.Destination, ct);
					item = item.WithSidecarPath(Path.Combine(plan.Destination, $"{item.Name}.sidecar"));
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Sidecar generation failed for {ItemName}", item.Name);
				}

				results.Add(item);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Transfer failed for {ItemName}", item.Name);
				throw;  // Critical error: stop backup
			}
		}

		_logger.LogInformation("Transfer complete: {ItemCount} items", results.Count);
		return results;
	}

	private async Task<List<BackupItem>> ExtractMetadata(
		List<BackupItem> items,
		IProgress<IBackupProgress>? progress,
		CancellationToken ct)
	{
		_logger.LogInformation("Phase: Extracting metadata");

		var results = new List<BackupItem>();

		for (int i = 0; i < items.Count; i++)
		{
			var item = items[i];

			progress?.Report(new BackupProgress
			{
				Phase = BackupPhase.ExtractingMetadata,
				CurrentFile = item.Name,
				FilesProcessed = i,
				FilesTotal = items.Count
			});

			try
			{
				var destPath = Path.Combine(item.DestinationPath);
				if (File.Exists(destPath))
				{
					var metadata = await _metadataReader.ReadAsync(destPath, ct);
					item = item.WithMetadata(metadata);
				}
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Metadata extraction failed for {ItemName}", item.Name);
				// Non-critical: continue
			}

			results.Add(item);
		}

		return results;
	}

	private async Task<List<BackupItem>> GenerateHashes(
		List<BackupItem> items,
		BackupPlan plan,
		IProgress<IBackupProgress>? progress,
		CancellationToken ct)
	{
		_logger.LogInformation("Phase: Generating hashes ({HashCount} types)", plan.HashTypes.Count);

		var results = new List<BackupItem>();

		for (int i = 0; i < items.Count; i++)
		{
			var item = items[i];

			progress?.Report(new BackupProgress
			{
				Phase = BackupPhase.GeneratingHashes,
				CurrentFile = item.Name,
				FilesProcessed = i,
				FilesTotal = items.Count
			});

			try
			{
				var hashes = await _itemHasher.ComputeHashesAsync(item, plan.HashTypes, null, ct);
				item = item.WithHashes(hashes);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Hash generation failed for {ItemName}", item.Name);
				// Non-critical: continue
			}

			results.Add(item);
		}

		return results;
	}

	private async Task<List<BackupItem>> CorrectTimestamps(
		List<BackupItem> items,
		IProgress<IBackupProgress>? progress,
		CancellationToken ct)
	{
		_logger.LogInformation("Phase: Correcting timestamps");

		var results = new List<BackupItem>();

		for (int i = 0; i < items.Count; i++)
		{
			var item = items[i];

			progress?.Report(new BackupProgress
			{
				Phase = BackupPhase.CorrectingTimestamps,
				CurrentFile = item.Name,
				FilesProcessed = i,
				FilesTotal = items.Count
			});

			try
			{
				if (File.Exists(item.DestinationPath))
				{
					File.SetCreationTime(item.DestinationPath, item.CreatedAt);
					File.SetLastWriteTime(item.DestinationPath, item.ModifiedAt);
				}
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Timestamp correction failed for {ItemName}", item.Name);
				// Non-critical: continue
			}

			results.Add(item);
		}

		return results;
	}

	private static BackupJobResult BuildSuccessResult(List<BackupItem> items, TimeSpan duration)
	{
		return new BackupJobResult
		{
			Success = true,
			TotalItems = items.Count,
			SuccessfulItems = items.Count,
			FailedItems = 0,
			TotalBytes = items.Sum(i => i.SizeInBytes),
			Duration = duration,
			Errors = new(),
			Items = items
		};
	}

	private static BackupJobResult BuildFailureResult(List<BackupItem> items, List<BackupError> errors, TimeSpan duration)
	{
		return new BackupJobResult
		{
			Success = false,
			TotalItems = items.Count,
			SuccessfulItems = items.Count(i => i.DestinationPath != null),
			FailedItems = items.Count(i => i.DestinationPath == null),
			TotalBytes = items.Sum(i => i.SizeInBytes),
			Duration = duration,
			Errors = errors,
			Items = items
		};
	}
}

/// <summary>
/// Concrete progress implementation for reporting backup progress.
/// </summary>
public class BackupProgress : IBackupProgress
{
	public string CurrentFile { get; init; } = "";
	public long BytesTransferred { get; init; }
	public int FilesProcessed { get; init; }
	public int FilesTotal { get; init; }
	public BackupPhase Phase { get; init; }
}
