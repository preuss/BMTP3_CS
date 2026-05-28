using BMTP3.Core4.Api;
using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Engine.DiskSpace;
using BMTP3.Core4.Engine.Downloader;
using BMTP3.Core4.Engine.Hashing;
using BMTP3.Core4.Engine.Session;
using BMTP3.Core4.Engine.Sidecar;
using BMTP3.Core4.Engine.TimeStamp;
using BMTP3.Core4.Engine.Validation;
using BMTP3.Core4.Models;
using BMTP3.Core4.Models.Enums;
using BMTP3.Core4.Scanner;
using BMTP3.Core4.State;
using BMTP3.Core4.Traversal;
using Microsoft.Extensions.Logging;

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
	private readonly IDownloadService _downloadService;
	private readonly IHashService _hashService;
	private readonly IEarliestTimestampResolutionService _earliestTimestampService;
	private readonly ISidecarService _sidecarService;
	private readonly IDiskSpaceValidator _diskSpaceValidator;
	private readonly ILogger<BackupEngine> _logger;


	internal BackupEngine(
		IBackupScanner scanner,
		ISourceTraversalFactory sourceTraversalFactory,
		IDownloadService downloadService,
		IHashService hashService,
		IEarliestTimestampResolutionService earliestTimestampService,
		ISidecarService sidecarService,
		IDiskSpaceValidator diskSpaceValidator,
		ILogger<BackupEngine> logger
	)
	{
		_scanner = scanner;
		_sourceTraversalFactory = sourceTraversalFactory;
		_downloadService = downloadService;
		_hashService = hashService;
		_earliestTimestampService = earliestTimestampService;
		_sidecarService = sidecarService;
		_diskSpaceValidator = diskSpaceValidator;
		_logger = logger;
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

		var summaryStore = new BackupJsonSummaryStore(metadataPath, sessionKey.SessionId);
		var sessionState = new SessionStateService(summaryStore);

		// ------------------------------------------------------------
		// 3b. Validate disk space — minimum free space for application
		//      (logs, metadata, temp files).
		// ------------------------------------------------------------
		await _diskSpaceValidator.EnsureMinimumFreeSpaceAsync(plan.Destination, cancellationToken);

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
			BackupRecord record = new()
			{
				Item = item,
				Metadata = new ItemMetadata
				{
					AuthoredDateTime = item.DateAuthored,
					CreatedDateTime = item.DateCreated,
					ModifiedDateTime = item.DateModified,
					AccessedDateTime = item.DateAccessed,
				},
			};

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

		await sessionState.ApplyResumeAsync(repository.GetAll(), sessionKey, plan.ResumeBehavior, cancellationToken);

		List<BackupRecord> pendingRecords = FilterPendingRecords(repository.GetAll(), ref _currentProgress, progress);

		// ------------------------------------------------------------
		// 5c. Validate disk space — sufficient capacity for backup content
		//      (total file size + overhead buffer).
		// ------------------------------------------------------------
		long totalBytesRequired = pendingRecords.Sum(r => (long)r.Item.Content.Length);
		await _diskSpaceValidator.EnsureSufficientBackupCapacityAsync(plan.Destination, totalBytesRequired, cancellationToken);

		// ------------------------------------------------------------
		// 6. Process pending items
		//    - Download content to temp file
		//    - Extract earliest timestamp from metadata
		//    - Compute hashes (comparison + verification)
		//    - Resolve destination path (collision handling)
		//    - Move file from temp to destination
		//    - Write sidecar file
		//    - Update progress (phase = Transferring / Hashing)
		// ------------------------------------------------------------

		DirectoryInfo sessionTempDir = TempDirectoryHelper.ResolveTempDirectoryPath(plan.Destination, backupStartTime, sessionKey);

		try
		{
			TempDirectoryHelper.PrepareTempDirectory(sessionTempDir);

			foreach(BackupRecord record in pendingRecords)
			{
				// Create temp file path for this item.
				FileInfo tempFile = TempDirectoryHelper.BuildTempFilePath(sessionTempDir, record.Item.FileName);

				// Download content to temp file with progress reporting.
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
				downloadProgress.Report(0);

				DownloadRequest downloadRequest = new()
				{
					Destination = tempFile,
					Item = record.Item,
					BackupStartTime = backupStartTime,
				};

				await _downloadService.DownloadAsync(
					downloadRequest,
					downloadProgress,
					cancellationToken);

				// Extract earliest authored timestamp from file metadata.
				EarliestTimestampResolutionResult earliest = await _earliestTimestampService.ResolveEarliestAsync(
					record.Item.Content, cancellationToken);

				if(earliest.Timestamp.HasValue)
				{
					record.Metadata.AuthoredDateTime = earliest.Timestamp;
					record.Metadata.CreatedDateTime = earliest.Timestamp;
				}

				// Compute hashes.
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
				computeHashProgress.Report(0);

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

				// ------------------------------------------------------------
				// Commit: resolve path → move file → write sidecar
				// ------------------------------------------------------------

				string destinationDir = Path.Combine(
					plan.Destination,
					Path.GetDirectoryName(record.Item.RelativePath) ?? string.Empty);

				string finalPath = CollisionHelpers.ResolveTargetPath(
					destinationDir,
					record.Item.FileName,
					plan.CollisionStrategy,
					out CollisionResolution resolution);

				if(resolution == CollisionResolution.Skip)
				{
					record.Status = BackupItemStatus.Skipped;
					continue;
				}

				if(resolution == CollisionResolution.Error)
				{
					throw new IOException($"Destination already exists: {finalPath}");
				}

				Directory.CreateDirectory(destinationDir);

				IMoveableContent moveableContent = (IMoveableContent)record.Item.Content;
				IContent movedContent = moveableContent.MoveTo(finalPath, overwrite: resolution == CollisionResolution.Overwrite);
				record.Item.ReplaceContentProvider(movedContent);
				record.DestinationPath = finalPath;

				if(plan.SidecarFormat != SidecarFormat.None)
				{
					SidecarRequest sidecarRequest = new()
					{
						Format = plan.SidecarFormat,
						OriginalFileName = record.Item.FileName,
						RelativePath = record.Item.RelativePath,
						CreateDateTime = record.Metadata.CreatedDateTime,
						AccessDateTime = record.Metadata.AccessedDateTime,
						ModifyDateTime = record.Metadata.ModifiedDateTime,
						AuthoredDateTime = record.Metadata.AuthoredDateTime,
						Hashes = record.Metadata.ComputedHashes,
						BackupStartTime = backupStartTime,
					};

					await _sidecarService.WriteAsync(finalPath, sidecarRequest, cancellationToken);
				}

				record.Status = BackupItemStatus.Succeeded;

				_currentProgress = _currentProgress with
				{
					ActiveFiles = Array.Empty<BackupProgressItem>(),
				};
				progress?.Report(_currentProgress);
			}
		} finally
		{
			await sessionState.SaveAsync(repository.GetAll(), sessionKey);

			// Do this even when exception or cancel.
			// Do not let cleanup errors mask original failure.
			if(sessionTempDir.Exists)
			{
				try
				{
					bool removed = TempDirectoryHelper.CleanupSessionTempDirectory(sessionTempDir);
					if(!removed)
					{
						_logger.LogDebug("Session temp directory not empty, kept: {sessionTempDir}", sessionTempDir.FullName);
					}
				} catch(Exception ex)
				{
					_logger.LogWarning(ex, "Could not clean session temp directory: {sessionTempDir}", sessionTempDir.FullName);
				}
			}
		}

		// ------------------------------------------------------------
		// 7. Final progress report
		// ------------------------------------------------------------

		IReadOnlyList<BackupRecord> allRecords = repository.GetAll();

		_currentProgress = _currentProgress with
		{
			CurrentPhase = BackupProgressPhase.Completed,
			FilesSucceeded = allRecords.Count(r => r.Status == BackupItemStatus.Succeeded),
			FilesSkipped = allRecords.Count(r => r.Status == BackupItemStatus.Skipped),
			FilesFailed = allRecords.Count(r => r.Status == BackupItemStatus.Failed),
		};
		progress?.Report(_currentProgress);

		// ------------------------------------------------------------
		// 8. Finalize backup result
		//    - Collect item results from all records
		//    - Determine final state (Completed / Failed / Cancelled)
		//    - Set failure reason if applicable
		// ------------------------------------------------------------



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
