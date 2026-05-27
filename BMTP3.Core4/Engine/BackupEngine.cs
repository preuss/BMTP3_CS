using BMTP3.Core4.Api;
using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Engine.Downloader;
using BMTP3.Core4.Engine.Hashing;
using BMTP3.Core4.Engine.Runner;
using BMTP3.Core4.Engine.Session;
using BMTP3.Core4.Engine.TimeStamp;
using BMTP3.Core4.Engine.Validation;
using BMTP3.Core4.Models;
using BMTP3.Core4.Models.Enums;
using BMTP3.Core4.Scanner;
using BMTP3.Core4.Traversal;

namespace BMTP3.Core4.Engine;

/// <summary>
/// Orchestrates the execution of a backup job.
/// This implementation defines the ordered steps of a backup run.
/// Each step is initially expressed as a comment and will later
/// be replaced with concrete implementation calls.
/// </summary>
public sealed class BackupEngine : IBackupEngine
{
	private readonly IBackupScanner _scanner;
	private readonly ISourceTraversalFactory _sourceTraversalFactory;
	private readonly IBackupRunnerFactory _backupRunnerFactory;
	private readonly ISessionStateService _sessionState;
	private readonly IDownloadService _downloadService;
	private readonly IHashService _hashService;
	private readonly IEarliestTimestampResolutionService _earliestTimestampService;

	private DirectoryInfo? _tempDir;

	internal BackupEngine(
		IBackupScanner scanner,
		ISourceTraversalFactory sourceTraversalFactory,
		IBackupRunnerFactory backupRunnerFactory,
		ISessionStateService sessionState,
		IDownloadService downloadService,
		IHashService hashService,
		IEarliestTimestampResolutionService earliestTimestampService
	)
	{
		_scanner = scanner;
		_sourceTraversalFactory = sourceTraversalFactory;
		_backupRunnerFactory = backupRunnerFactory;
		_sessionState = sessionState;
		_downloadService = downloadService;
		_hashService = hashService;
		_earliestTimestampService = earliestTimestampService;
	}

	public async Task<BackupResult> RunAsync(
		BackupPlan plan,
		IProgress<BackupProgress>? progress,
		CancellationToken cancellationToken
	)
	{
		// ------------------------------------------------------------
		// 1. Validate backup plan
		//    - Ensure required fields are present
		//    - Ensure source and destination are not the same
		//    - Fail fast on invalid configuration
		// ------------------------------------------------------------
		BackupPlanValidator.Validate(plan);

		// ------------------------------------------------------------
		// 2. Initialize backup state
		//    - Create internal state objects
		//    - Initialize progress tracking
		//    - Set phase = Starting
		// ------------------------------------------------------------

		BackupProgress _currentProgress = new()
		{
			SourcePath = plan.SourcePath,
			DestinationPath = plan.Destination,
			CurrentPhase = BackupProgressPhase.Starting,
		};
		progress?.Report(_currentProgress);

		DateTimeOffset backupStartTime = DateTimeOffset.UtcNow;

		IBackupRecordRepository repository = new BackupMemoryRecordRepository();

		BackupSessionKey sessionKey = BackupSessionKeyFactory.Create(plan);

		// ------------------------------------------------------------
		// 3. Prepare destination
		//    - Create root destination folder
		//    - Create .bmtp3 subfolder for session data (session.json, logs)
		//    - Fail if destination is not accessible
		// ------------------------------------------------------------
		Directory.CreateDirectory(plan.Destination);
		string metadataPath = Path.Combine(plan.Destination, ".bmtp3");
		Directory.CreateDirectory(metadataPath);

		// ------------------------------------------------------------
		// 4. Open source traversal (filesystem or media device)
		//    - Establish access to source via ISourceTraversal
		//    - Fail if source is not accessible
		// ------------------------------------------------------------
		SourceTraversalFactoryCreateRequest sourceTraversalFactoryCreateRequest = new()
		{
			SourceType = plan.SourceType,
			SourcePath = plan.SourcePath,
		};

		await using ISourceTraversal traversal = _sourceTraversalFactory.Create(sourceTraversalFactoryCreateRequest);

		// ------------------------------------------------------------
		// 5. Scan source
		//    - Enumerate directories and files
		//    - Apply include / exclude rules
		//    - Count files and total bytes
		//    - Update progress (phase = Scanning)
		// ------------------------------------------------------------

		BackupScanRequest scanRequest = new()
		{
			SourcePath = plan.SourcePath,
			Recursive = plan.Recursive,
			IncludePatterns = plan.IncludePatterns,
			ExcludePatterns = plan.ExcludePatterns,
		};

		Progress<BackupScanProgress> scanProgress = new(sp =>
		{
			_currentProgress = _currentProgress with
			{
				CurrentPhase = BackupProgressPhase.Scanning,
				DirectoriesTraversed = sp.DirectoriesTraversed,
				FilesDiscovered = sp.FilesDiscovered,
			};
			progress?.Report(_currentProgress);
		});

		await foreach(BackupItem item in _scanner.ScanAsync(traversal, scanRequest, scanProgress, cancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			BackupRecord record = new() { Item = item };

			repository.Add(record);
		}

		// Update progress with total selected files.
		_currentProgress = _currentProgress with
		{
			TotalFilesSelected = repository.GetAll().Count,
		};
		progress?.Report(_currentProgress);

		// ------------------------------------------------------------
		// 5b. Resume — match scanned items against persisted summary
		//      to restore DestinationPath and processing Status
		// ------------------------------------------------------------

		await _sessionState.ApplyResumeAsync(repository.GetAll(), sessionKey, plan.ResumeBehavior, cancellationToken);

		List<BackupRecord> pendingRecords = FilterPendingRecords(repository.GetAll(), ref _currentProgress, progress);

		// Prepare temp directory for staged file transfer.
		DirectoryInfo tempDir = TempDirectoryHelper.ResolveTempDirectoryPath(plan.Destination, backupStartTime, sessionKey);
		TempDirectoryHelper.PrepareTempDirectory(tempDir);

		_tempDir = tempDir;

		IBackupRunner runner = _backupRunnerFactory.Create(new BackupRunnerFactoryCreateRequest());

		// ------------------------------------------------------------
		// 6. Process pending items
		//    - Download content to temp file
		//    - Compute hashes (comparison + verification)
		//    - Transfer files: copy from source to destination via runner
		//    - Generate sidecar files via runner
		//    - Update progress (phase = Transferring / Hashing)
		// ------------------------------------------------------------

		foreach(BackupRecord record in pendingRecords)
		{
			// Create temp file path for this item
			FileInfo tempFile = TempDirectoryHelper.BuildTempFilePath(tempDir, record.Item.FileName);

			// Download content to temp file with progress reporting
			BackupProgressItem currentProgressItem = new()
			{
				RelativePath = record.Item.RelativePath,
				Length = (long)record.Item.Content.Length,
				Phase = BackupProgressItemPhase.Transferring,
			};
			_currentProgress = _currentProgress with { ActiveFiles = new[] { currentProgressItem } };
			progress?.Report(_currentProgress);

			IProgress<ulong> downloadProgress = new Progress<ulong>(bytesRead =>
			{
				currentProgressItem = currentProgressItem with { BytesProcessed = (long)bytesRead };
				_currentProgress = _currentProgress with { ActiveFiles = new[] { currentProgressItem } };
				progress?.Report(_currentProgress);
			});
			// Initialize progress with 0 bytes read to show the item in the UI immediately.
			downloadProgress.Report(0);

			IMoveableContent content = await _downloadService.DownloadAsync(
				tempFile,
				record.Item.Content,
				downloadProgress,
				cancellationToken);
			record.Item.ReplaceContentProvider(content);

			EarliestTimestampResolutionResult earliest = await _earliestTimestampService.ResolveEarliestAsync(
				content, cancellationToken);

			if(earliest.Timestamp.HasValue)
			{
				record.Metadata.AuthoredDateTime = earliest.Timestamp;
				record.Metadata.CreatedDateTime = earliest.Timestamp;
			}

			BackupRunnerRequest runnerRequest = new()
			{
				SidecarFormat = plan.SidecarFormat,
				CollisionStrategy = plan.CollisionStrategy,
				BackupStartTime = backupStartTime,
			};

			IProgress<ulong> computeHashProgress = new Progress<ulong>(bytesComputed =>
			{
				currentProgressItem = currentProgressItem with
				{
					BytesProcessed = (long)bytesComputed,
					Phase = BackupProgressItemPhase.Hashing,
				};
				_currentProgress = _currentProgress with { ActiveFiles = new[] { currentProgressItem } };
				progress?.Report(_currentProgress);
			});

			// Initialize progress with 0 bytes computed to update the phase in the UI immediately.
			computeHashProgress.Report(0);

			// Compute for all types of hashes required by the plan.
			List<HashAlgorithmType> allAlgorithms =
				plan.ComparisonHashAlgorithmTypes!
				.Concat(plan.VerificationHashAlgorithmTypes!)
				.Distinct()
				.ToList();

			record.Metadata.ComputedHashes = await _hashService.ComputeHashesAsync(
				record.Item.Content,
				record.Item.RelativePath,
				allAlgorithms,
				computeHashProgress,
				cancellationToken);

			record.DestinationPath = Path.Combine(plan.Destination, record.Item.RelativePath);

			BackupResultItem runnerResult = await runner.RunAsync(
				record,
				tempFile,
				runnerRequest,
				cancellationToken);

			record.Status = runnerResult.State switch
			{
				BackupResultItemState.Succeeded => BackupItemStatus.Succeeded,
				BackupResultItemState.Skipped => BackupItemStatus.Skipped,
				_ => BackupItemStatus.Failed,
			};
			record.DestinationPath = runnerResult.DestinationPath;

			_currentProgress = _currentProgress with
			{
				ActiveFiles = Array.Empty<BackupProgressItem>(),
			};
			progress?.Report(_currentProgress);
		}

		// ------------------------------------------------------------
		// 7. (Future) Optional per-item features
		//    - Metadata extraction
		//    - Post-write verification
		//    - Timestamp correction
		// ------------------------------------------------------------

		// ------------------------------------------------------------
		// 8. Handle cancellation
		//    - Observe cancellationToken
		//    - Stop processing gracefully if requested
		// ------------------------------------------------------------

		// ------------------------------------------------------------
		// 9. Finalize backup result
		//    - Collect item results from all records
		//    - Determine final state (Completed / Failed / Cancelled)
		//    - Set failure reason if applicable
		// ------------------------------------------------------------

		IReadOnlyList<BackupRecord> allRecords = repository.GetAll();

		List<BackupResultItem> itemResults = new(allRecords.Count);
		bool anyFailed = false;

		foreach(BackupRecord record in allRecords)
		{
			itemResults.Add(new BackupResultItem
			{
				Id = record.Item.Id,
				SourcePath = record.Item.SourcePath,
				DestinationPath = record.DestinationPath,
				Length = (long)record.Item.Content.Length,
				State = MapItemState(record.Status),
			});

			if(record.Status == BackupItemStatus.Failed)
				anyFailed = true;
		}

		BackupResult result = new()
		{
			Name = plan.Name,
			State = anyFailed ? BackupResultState.Failed : BackupResultState.Completed,
			ItemResults = itemResults.AsReadOnly(),
		};

		// ------------------------------------------------------------
		// 10. Return BackupResult
		// ------------------------------------------------------------

		return result;
	}

	private static BackupResultItemState MapItemState(BackupItemStatus status) => status switch
	{
		BackupItemStatus.Succeeded => BackupResultItemState.Succeeded,
		BackupItemStatus.Failed => BackupResultItemState.Failed,
		BackupItemStatus.Skipped => BackupResultItemState.Skipped,
		_ => BackupResultItemState.Skipped,
	};

	private static List<BackupRecord> FilterPendingRecords(
		IReadOnlyList<BackupRecord> records,
		ref BackupProgress currentProgress,
		IProgress<BackupProgress>? progress)
	{
		List<BackupRecord> pending = new();

		foreach(BackupRecord record in records)
		{
			switch(record.Status)
			{
				case BackupItemStatus.Succeeded:
					currentProgress = currentProgress with { FilesSucceeded = currentProgress.FilesSucceeded + 1 };
					progress?.Report(currentProgress);
					break;

				case BackupItemStatus.Skipped:
					currentProgress = currentProgress with { FilesSkipped = currentProgress.FilesSkipped + 1 };
					progress?.Report(currentProgress);
					break;

				case BackupItemStatus.Pending:
					pending.Add(record);
					break;
			}
		}

		return pending;
	}
}
