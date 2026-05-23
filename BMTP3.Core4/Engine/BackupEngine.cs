using BMTP3.Core4.Api;
using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Engine.Downloader;
using BMTP3.Core4.Engine.Runner;
using BMTP3.Core4.Engine.Session;
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

	internal BackupEngine(
		IBackupScanner scanner,
		ISourceTraversalFactory sourceTraversalFactory,
		IBackupRunnerFactory backupRunnerFactory,
		ISessionStateService sessionState,
		IDownloadService downloadService
	)
	{
		_scanner = scanner;
		_sourceTraversalFactory = sourceTraversalFactory;
		_backupRunnerFactory = backupRunnerFactory;
		_sessionState = sessionState;
		_downloadService = downloadService;
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
		// 5. Prepare destination
		//    - Create root destination folder
		//    - Create .bmtp3 subfolder for session data (session.json, logs)
		//    - Fail if destination is not accessible
		// ------------------------------------------------------------
		Directory.CreateDirectory(plan.Destination);
		string metadataPath = Path.Combine(plan.Destination, ".bmtp3");
		Directory.CreateDirectory(metadataPath);

		// ------------------------------------------------------------
		// 3. Open source traversal (filesystem or media device)
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
		// 4. Scan source
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
		// 4b. Resume — match scanned items against persisted summary
		//      to restore DestinationPath and processing Status
		// ------------------------------------------------------------

		await _sessionState.ApplyResumeAsync(repository.GetAll(), sessionKey, plan.ResumeBehavior, cancellationToken);

		List<BackupRecord> pendingRecords = FilterPendingRecords(repository.GetAll(), ref _currentProgress, progress);

		// TODO: Validate DestinationPath on all records. Then loop over pendingRecords:
		//   - Prepare temp dir / temp file path
		//   - _downloadService.DownloadAsync(tempFile, item.Content, ...)
		//   - item.ReplaceContentProvider(moveableContent)
		//   - call single-item IBackupRunner.RunAsync(...)
		// Build BackupResultItem list at the end from all records (Succeeded/Skipped from resume + results from loop).



		BackupRunnerFactoryCreateRequest runnerFactoryCreateRequest = new()
		{
			MaxDegreeOfParallelism = plan.MaxDegreeOfParallelism
		};
		IBackupRunner runner = _backupRunnerFactory.Create(runnerFactoryCreateRequest);

		BackupRunnerRequest runnerRequest = new()
		{
			Records = repository.GetAll(),
			Destination = plan.Destination,
			OutputStructureStrategy = plan.OutputStructureStrategy,
			CollisionStrategy = plan.CollisionStrategy,
			SidecarFormat = plan.SidecarFormat,
			StopOnError = plan.StopOnError,
			BackupStartTime = backupStartTime,
		};

		Progress<BackupRunnerProgress> runnerProgress = new(rp =>
		{
			_currentProgress = _currentProgress with
			{
				CurrentPhase = rp.CurrentPhase,
				FilesSucceeded = rp.FilesSucceeded,
				FilesSkipped = rp.FilesSkipped,
				FilesFailed = rp.FilesFailed,
				ActiveFiles = rp.ActiveFiles,
			};
			progress?.Report(_currentProgress);
		});

		await runner.RunAsync(runnerRequest, sessionKey, runnerProgress, cancellationToken);


		// ------------------------------------------------------------
		// 6. Transfer files
		//    - Copy data from source to destination
		//    - Generate sidecar files
		//    - Update progress (phase = Transferring)
		// ------------------------------------------------------------

		// ------------------------------------------------------------
		// 7. Execute optional features (if enabled in BackupPlan)
		//    - Hashing
		//    - Metadata extraction
		//    - Verification
		//    - Timestamp correction
		// ------------------------------------------------------------

		// ------------------------------------------------------------
		// 8. Handle cancellation
		//    - Observe cancellationToken
		//    - Stop processing gracefully if requested
		// ------------------------------------------------------------

		// ------------------------------------------------------------
		// 9. Finalize backup result
		//    - Collect final counters
		//    - Determine final phase (Completed / Failed / Cancelled)
		//    - Set failure reason if applicable
		// ------------------------------------------------------------

		// ------------------------------------------------------------
		// 10. Return BackupResult
		// ------------------------------------------------------------

		throw new NotImplementedException("BackupEngine is not yet implemented.");
	}

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
