using BMTP3.Core4.Api;
using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Engine.Downloader;
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
	private readonly IMetadataTimestampService _metadataTimestampService;

	private DirectoryInfo? _tempDir;

	internal BackupEngine(
		IBackupScanner scanner,
		ISourceTraversalFactory sourceTraversalFactory,
		IBackupRunnerFactory backupRunnerFactory,
		ISessionStateService sessionState,
		IDownloadService downloadService,
		IMetadataTimestampService metadataTimestampService
	)
	{
		_scanner = scanner;
		_sourceTraversalFactory = sourceTraversalFactory;
		_backupRunnerFactory = backupRunnerFactory;
		_sessionState = sessionState;
		_downloadService = downloadService;
		_metadataTimestampService = metadataTimestampService;
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

		// Prepare temp directory for staged file transfer.
		DirectoryInfo tempDir = TempDirectoryHelper.ResolveTempDirectoryPath(plan.Destination, backupStartTime, sessionKey);
		TempDirectoryHelper.PrepareTempDirectory(tempDir);

		_tempDir = tempDir;

		// TODO: Loop over pendingRecords — download + run + collect results:
		//   foreach(BackupRecord record in pendingRecords)
		//   {
		//       string tempFile = Path.Combine(tempDir.FullName, TempDirectoryHelper.BuildTempFileName(record.Item.FileName));
		//       IMoveableContent content = await _downloadService.DownloadAsync(
		//           new FileInfo(tempFile), record.Item.Content, null, cancellationToken);
		//       record.Item.ReplaceContentProvider(content);
		//       BackupRunnerRequest runnerRequest = new()
		//       {
		//           SidecarFormat = plan.SidecarFormat,
		//           CollisionStrategy = plan.CollisionStrategy,
		//           BackupStartTime = backupStartTime,
		//       };
		//       BackupResultItem result = new SequentialBackupRunner().RunAsync(
		//           record.Item, record.DestinationPath!, tempFile, runnerRequest, cancellationToken);
		//       results.Add(result);
		//   }
		// Build BackupResultItem list at the end from all records
		// (Succeeded/Skipped from resume + results from loop).


		IBackupRunner runner = _backupRunnerFactory.Create(new BackupRunnerFactoryCreateRequest());

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

			Progress<ulong> downloadProgress = new(bytesRead =>
			{
				currentProgressItem = currentProgressItem with { BytesProcessed = (long)bytesRead };
				_currentProgress = _currentProgress with { ActiveFiles = new[] { currentProgressItem } };
				progress?.Report(_currentProgress);
			});
			IMoveableContent content = await _downloadService.DownloadAsync(
				tempFile,
				record.Item.Content,
				downloadProgress,
				cancellationToken);
			record.Item.ReplaceContentProvider(content);

			BackupRunnerRequest runnerRequest = new()
			{
				SidecarFormat = plan.SidecarFormat,
				CollisionStrategy = plan.CollisionStrategy,
				BackupStartTime = backupStartTime,
			};

			await runner.RunAsync(
				record.Item,
				new FileInfo(record.DestinationPath!),
				tempFile,
				runnerRequest,
				cancellationToken);

			record.Status = BackupItemStatus.Succeeded;

			_currentProgress = _currentProgress with
			{
				ActiveFiles = Array.Empty<BackupProgressItem>(),
			};
			progress?.Report(_currentProgress);
		}

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
