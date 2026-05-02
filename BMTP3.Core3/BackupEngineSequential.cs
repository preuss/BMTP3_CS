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
		ILogger<BackupEngineSequential> logger
	)
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
		CancellationToken ct
	)
	{
		ArgumentNullException.ThrowIfNull(plan);
		ct.ThrowIfCancellationRequested();

		Stopwatch stopwatch = Stopwatch.StartNew();
		List<BackupError> errors = new();
		List<BackupItem> items = new();

		try
		{
			_logger.LogInformation("Starting backup: {Source} → {Destination}", plan.Source, plan.Destination);

			// Step 1: Scan
			items = await Scan(plan, progress, ct);

			// Step 2: Transfer + Sidecar (per-file operation)
			items = await TransferAndGenerateSidecars(items, plan, progress, ct);

			// Step 3: Extract Metadata (non-critical failures)
			items = await ExtractMetadata(items, plan, progress, ct);

			// Step 4: Generate Hashes (non-critical failures)
			items = await GenerateHashes(items, plan, progress, ct);

			// Step 5: Correct Timestamps (non-critical failures)
			items = await CorrectTimestamps(items, plan, progress, ct);

			stopwatch.Stop();

			_logger.LogInformation("Backup complete: {ItemCount} items in {Duration}ms", items.Count, stopwatch.ElapsedMilliseconds);

			return BuildSuccessResult(items, stopwatch.Elapsed);
		} catch(OperationCanceledException ex)
		{
			stopwatch.Stop();
			_logger.LogWarning("Backup cancelled after {Duration}ms", stopwatch.ElapsedMilliseconds);
			errors.Add(new BackupError { ItemName = "Backup", Message = "Operation cancelled", Exception = ex });
			return BuildFailureResult(items, errors, stopwatch.Elapsed);
		} catch(Exception ex)
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

		IEnumerable<BackupItem> scannedItems = await _scanner.ScanAsync(plan.Source, ct);
		List<BackupItem> items = scannedItems.ToList();

		// Report final scan result so consumers know the total item count
		progress?.Report(new BackupProgress
		{
			Phase = BackupPhase.Scanning,
			CurrentFile = plan.Source,
			FilesTotal = items.Count,
			FilesProcessed = items.Count
		});

		_logger.LogInformation("Scan complete: {ItemCount} items found", items.Count);
		return items;
	}

	private async Task<List<BackupItem>> TransferAndGenerateSidecars(
		List<BackupItem> items,
		BackupPlan plan,
		IProgress<IBackupProgress>? progress,
		CancellationToken ct
	)
	{
		_logger.LogInformation("Phase: Transfer (with sidecars)");

		List<BackupItem> results = new();

		for(int i = 0; i < items.Count; i++)
		{
			ct.ThrowIfCancellationRequested();

			BackupItem item = items[i];

			progress?.Report(new BackupProgress
			{
				Phase = BackupPhase.Transferring,
				CurrentFile = item.Name,
				FilesProcessed = i + 1,
				FilesTotal = items.Count
			});

			try
			{
				if(!plan.DryRun)
				{
					// Transfer file (critical: fail if this fails)
					await _fileTransfer.CopyAsync(item, plan.Destination, null, ct);

					// Update destination path - may differ from source name due to collision resolution
					string destPath = Path.Combine(plan.Destination, item.Name);
					item = item.WithDestinationPath(destPath).WithSuccess();

					// Generate sidecar immediately after transfer (non-critical: tolerate failure)
					try
					{
						await _sidecarGenerator.GenerateAsync(item, plan.Destination, ct);
						item = item.WithSidecarPath(Path.Combine(plan.Destination, $"{item.Name}.sidecar"));
					} catch(OperationCanceledException)
					{
						throw;
					} catch(Exception ex)
					{
						_logger.LogWarning(ex, "Sidecar generation failed for {ItemName}", item.Name);
					}
				}

				// DryRun: item remains Pending, DestinationPath is not set

				results.Add(item);
			} catch(OperationCanceledException)
			{
				throw;
			} catch(Exception ex)
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
		BackupPlan plan,
		IProgress<IBackupProgress>? progress,
		CancellationToken ct
	)
	{
		_logger.LogInformation("Phase: Extracting metadata");

		List<BackupItem> results = new();

		for(int i = 0; i < items.Count; i++)
		{
			ct.ThrowIfCancellationRequested();

			BackupItem item = items[i];

			progress?.Report(new BackupProgress
			{
				Phase = BackupPhase.ExtractingMetadata,
				CurrentFile = item.Name,
				FilesProcessed = i + 1,
				FilesTotal = items.Count
			});

			try
			{
				// Skip metadata extraction in dry run - file does not exist on disk
				if(!plan.DryRun)
				{
					string destPath = Path.Combine(item.DestinationPath);
					if(File.Exists(destPath))
					{
						Dictionary<string, object> metadata = await _metadataReader.ReadAsync(destPath, ct);
						item = item.WithMetadata(metadata);
					}
				}
			} catch(OperationCanceledException)
			{
				throw;
			} catch(Exception ex)
			{
				_logger.LogWarning(ex, "Metadata extraction failed for {ItemName}", item.Name);
				// Non-critical: continue
			}

			results.Add(item);
		}

		_logger.LogInformation("Metadata extraction complete: {ItemCount} items", results.Count);
		return results;
	}

	private async Task<List<BackupItem>> GenerateHashes(
		List<BackupItem> items,
		BackupPlan plan,
		IProgress<IBackupProgress>? progress,
		CancellationToken ct
	)
	{
		_logger.LogInformation("Phase: Generating hashes ({HashCount} types)", plan.HashTypes.Count);

		List<BackupItem> results = new();

		for(int i = 0; i < items.Count; i++)
		{
			ct.ThrowIfCancellationRequested();

			BackupItem item = items[i];

			progress?.Report(new BackupProgress
			{
				Phase = BackupPhase.GeneratingHashes,
				CurrentFile = item.Name,
				FilesProcessed = i + 1,
				FilesTotal = items.Count
			});

			try
			{
				// Skip hash generation in dry run - file does not exist on disk
				if(!plan.DryRun)
				{
					Dictionary<HashType, string> hashes = await _itemHasher.ComputeHashesAsync(item, plan.HashTypes, null, ct);
					item = item.WithHashes(hashes);
				}
			} catch(OperationCanceledException)
			{
				throw;
			} catch(Exception ex)
			{
				_logger.LogWarning(ex, "Hash generation failed for {ItemName}", item.Name);
				// Non-critical: continue
			}

			results.Add(item);
		}

		_logger.LogInformation("Hash generation complete: {ItemCount} items", results.Count);
		return results;
	}

	private async Task<List<BackupItem>> CorrectTimestamps(
		List<BackupItem> items,
		BackupPlan plan,
		IProgress<IBackupProgress>? progress,
		CancellationToken ct
	)
	{
		_logger.LogInformation("Phase: Correcting timestamps");

		List<BackupItem> results = new();

		for(int i = 0; i < items.Count; i++)
		{
			ct.ThrowIfCancellationRequested();

			BackupItem item = items[i];

			progress?.Report(new BackupProgress
			{
				Phase = BackupPhase.CorrectingTimestamps,
				CurrentFile = item.Name,
				FilesProcessed = i + 1,
				FilesTotal = items.Count
			});

			try
			{
				// Skip timestamp correction in dry run - file does not exist on disk
				if(!plan.DryRun)
				{
					string destPath = Path.Combine(item.DestinationPath);
					if(File.Exists(destPath))
					{
						await Task.Run(() =>
						{
							File.SetCreationTime(destPath, item.CreatedAt);
							File.SetLastWriteTime(destPath, item.ModifiedAt);
						}, ct);
					}
				}
			} catch(OperationCanceledException)
			{
				throw;
			} catch(Exception ex)
			{
				_logger.LogWarning(ex, "Timestamp correction failed for {ItemName}", item.Name);
				// Non-critical: continue
			}

			results.Add(item);
		}

		_logger.LogInformation("Timestamp correction complete: {ItemCount} items", results.Count);
		return results;
	}

	private static BackupJobResult BuildSuccessResult(List<BackupItem> items, TimeSpan duration)
	{
		return new BackupJobResult
		{
			Success = true,
			TotalItems = items.Count,
			SuccessfulItems = items.Count(i => i.ResultState == BackupItemResultState.Success),
			FailedItems = items.Count(i => i.ResultState == BackupItemResultState.Failed),
			TotalBytes = items
				.Where(i => i.ResultState == BackupItemResultState.Success)
				.Sum(i => i.SizeInBytes),
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
			SuccessfulItems = items.Count(i => i.ResultState == BackupItemResultState.Success),
			FailedItems = items.Count(i => i.ResultState == BackupItemResultState.Failed),
			TotalBytes = items
				.Where(i => i.ResultState == BackupItemResultState.Success)
				.Sum(i => i.SizeInBytes),
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
