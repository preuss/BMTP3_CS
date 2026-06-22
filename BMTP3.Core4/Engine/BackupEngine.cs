using BMTP3.Core4.Api;
using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.DriveDiscovery;
using BMTP3.Core4.Engine.DiskSpace;
using BMTP3.Core4.Engine.Downloader;
using BMTP3.Core4.Engine.Hashing;
using BMTP3.Core4.Engine.Index;
using BMTP3.Core4.Engine.Session;
using BMTP3.Core4.Engine.Sidecar;
using BMTP3.Core4.Engine.Strategies;
using BMTP3.Core4.Engine.TimeStamp;
using BMTP3.Core4.Engine.Validation;
using BMTP3.Core4.Hashing;
using BMTP3.Core4.Helpers;
using BMTP3.Core4.Infrastructure.Throttling;
using BMTP3.Core4.Models;
using BMTP3.Core4.Models.Enums;
using BMTP3.Core4.Scanner;
using BMTP3.Core4.SignalInterrupts;
using BMTP3.Core4.State;
using BMTP3.Core4.Storage;
using BMTP3.Core4.Traversal;
using Microsoft.Extensions.Logging;

namespace BMTP3.Core4.Engine;

/// <summary>
/// Orchestrates the execution of a backup job.
/// This implementation defines the ordered steps of a backup run.
/// Each step is initially expressed as a comment and will later
/// be replaced with concrete implementation calls.
/// </summary>
internal sealed class BackupEngine : IBackupEngine
{
	private readonly IBackupScanner _scanner;
	private readonly ISourceTraversalFactory _sourceTraversalFactory;
	private readonly IDriveProvider _driveProvider;
	private readonly ISourceConnector _sourceConnector;
	private readonly IDownloadService _downloadService;
	private readonly IHashService _hashService;
	private readonly IEarliestTimestampResolutionService _earliestTimestampService;
	private readonly ISidecarService _sidecarService;
	private readonly IDiskSpaceValidator _diskSpaceValidator;
	private readonly ILogger<BackupEngine> _logger;
	private readonly ITargetPathResolver _targetPathResolver;
	private readonly ICollisionResolver _collisionResolver;
	private readonly IBackupIndexWriter _backupIndexWriter;


	internal BackupEngine(
		IBackupScanner scanner,
		ISourceTraversalFactory sourceTraversalFactory,
		IDriveProvider driveProvider,
		ISourceConnector sourceConnector,
		IDownloadService downloadService,
		IHashService hashService,
		IEarliestTimestampResolutionService earliestTimestampService,
		ISidecarService sidecarService,
		IDiskSpaceValidator diskSpaceValidator,
		ILogger<BackupEngine> logger,
		ITargetPathResolver targetPathResolver,
		ICollisionResolver collisionResolver,
		IBackupIndexWriter backupIndexWriter
	)
	{
		_scanner = scanner;
		_sourceTraversalFactory = sourceTraversalFactory;
		_driveProvider = driveProvider;
		_sourceConnector = sourceConnector;
		_downloadService = downloadService;
		_hashService = hashService;
		_earliestTimestampService = earliestTimestampService;
		_sidecarService = sidecarService;
		_diskSpaceValidator = diskSpaceValidator;
		_logger = logger;
		_targetPathResolver = targetPathResolver;
		_collisionResolver = collisionResolver;
		_backupIndexWriter = backupIndexWriter;
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

		CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		cancellationToken = cancellationTokenSource.Token; // Shadow callers token.

		// Finally saves when Cancel() is called and OperationCanceledException (OCE) is thrown.
		using ISignalSubscription signalRegistration = SignalInterrupt.On(SignalInterruptKind.All).Handler(context =>
		{
			cancellationTokenSource.Cancel();
		}).Create();

		IThrottler throttler = ThrottlerFactory.Create(plan.Delay, cancellationToken);

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

		BackupJsonSummaryStore summaryStore = new(metadataPath, sessionKey.SessionId);
		SessionStateService sessionState = new(summaryStore);

		try
		{
			// ------------------------------------------------------------
			// 3b. Validate disk space — minimum free space for application
			//      (logs, metadata, temp files).
			// ------------------------------------------------------------
			await _diskSpaceValidator.EnsureMinimumFreeSpaceAsync(plan.Destination, cancellationToken);

			// ------------------------------------------------------------
			// 4. Discover source drive and establish connection
			//    - List all available drives from all providers
			//    - Match the drive matching plan.SourcePath
			//    - Connect to the source via ISourceConnector
			//    - Create ISourceTraversal from the connected source
			//    - Fail if source is not accessible
			// ------------------------------------------------------------
			IReadOnlyList<IBackupDriveInfo> drives = _driveProvider.ListDrives();

			string internalSourcePath = PathHelper.ToInternalCanonicalUri(plan.SourcePath, plan.SourceType);
			IBackupDriveInfo matchedDrive = MatchDrive(drives, internalSourcePath)
				?? throw new InvalidOperationException($"No drive found matching source path '{plan.SourcePath}'.");

			using IConnectedSource connectedSource = _sourceConnector.Connect(matchedDrive);
			ISourceTraversal traversal = _sourceTraversalFactory.Create(connectedSource);

			// Extract source-level details (device or drive info) from the matched drive.
			BackupSourceDetails sourceDetails = matchedDrive switch
			{
				IBackupMediaDriveInfo m => new MediaDeviceDriveSourceDetails
				{
					DeviceId = m.DeviceId,
					Description = m.Description,
					FriendlyName = m.FriendlyName,
					Manufacturer = m.Manufacturer,
					Model = m.Model,
					SerialNumber = m.SerialNumber,
					FirmwareVersion = m.FirmwareVersion,
					DriveName = m.DriveName,
					VolumeLabel = m.VolumeLabel,
					DriveFormat = m.DriveFormat,
				},
				IBackupFileSystemDriveInfo f => new FileSystemDriveSourceDetails
				{
					DriveName = f.DriveName,
					VolumeLabel = f.VolumeLabel,
					DriveFormat = f.DriveFormat,
				},
				_ => throw new InvalidOperationException(
					$"Unsupported backup source drive type: {matchedDrive.GetType().FullName}"),
			};

			// ------------------------------------------------------------
			// 5. Scan source
			//    - Enumerate directories and files
			//    - Apply include / exclude rules
			//    - Count files and total bytes
			//    - Update progress (phase = Scanning)
			// ------------------------------------------------------------

			BackupScanRequest scanRequest = new()
			{
				SourcePath = internalSourcePath,
				Recursive = plan.Recursive,
				IncludePatterns = plan.IncludePatterns,
				ExcludePatterns = plan.ExcludePatterns,
				ItemIdScope = plan.ItemIdScope,
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

			await foreach (BackupItem item in _scanner.ScanAsync(traversal, scanRequest, scanProgress, cancellationToken))
			{
				BackupRecord record = new()
				{
					Item = item,
					SourceDetails = sourceDetails,
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

			_currentProgress = _currentProgress with { TotalBytesSelected = totalBytesRequired };
			progress?.Report(_currentProgress);

			// ------------------------------------------------------------
			// 5d. Dry-run — report discovered items, skip writes
			// ------------------------------------------------------------

			if (plan.DryRun) return BuildDryRunResult(repository, _currentProgress, progress, plan);

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

				_currentProgress = _currentProgress with { CurrentPhase = BackupProgressPhase.Transferring };
				progress?.Report(_currentProgress);

				foreach (BackupRecord record in pendingRecords)
				{
					FileInfo? tempFile = null;
					try
					{
						// Create temp file path for this item.
						tempFile = TempDirectoryHelper.BuildTempFilePath(sessionTempDir, record.Item.FileName);

						// Guard: RelativeFilePath is guaranteed non-null by the scanner, but flow analysis needs runtime evidence.
						if (record.Item.RelativeFilePath == null) throw new InvalidOperationException("Item RelativeFilePath is null.");

						// Download content to temp file with progress reporting.
						BackupProgressItem currentProgressItem = new()
						{
							RelativeFilePath = record.Item.RelativeFilePath,
							Length = (long)record.Item.Content.Length,
							Phase = BackupProgressItemPhase.Transferring,
						};
						_currentProgress = _currentProgress with { ActiveFiles = new[] { currentProgressItem } };
						progress?.Report(_currentProgress);

						IProgress<ulong> downloadProgress = new ActionProgress<ulong>(bytesRead =>
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
							cancellationToken
						);

						await throttler.WaitAsync();

						// Capture original source dates before timestamp correction overwrites them.
						record.Metadata.AuthoredDateTime = record.Item.DateAuthored;
						record.Metadata.CreatedDateTime = record.Item.DateCreated;
						record.Metadata.ModifiedDateTime = record.Item.DateModified;
						record.Metadata.AccessedDateTime = record.Item.DateAccessed;

						EarliestTimestampResolutionRequest earliestTimestampRequest = new()
						{
							Content = record.Item.Content,
							TimestampCorrectionTarget = tempFile,
							Item = record.Item,
							Metadata = record.Metadata,
						};

						EarliestTimestampResolutionResult earliest = await _earliestTimestampService.ResolveAndApplyEarliestAsync(
							earliestTimestampRequest,
							plan.EnableTimestampCorrection,
							cancellationToken
						);

						// Need a value here to proceed. If timestamp correction is disabled, we still want to use the original metadata timestamps if available.
						if (!earliest.Timestamp.HasValue)
						{
							throw new InvalidOperationException($"Could not resolve valid timestamp for '{record.Item.SourcePath}'.");
						}

						DateTimeOffset createFileDate = earliest.Timestamp.Value;

						// Compute hashes.
						IProgress<ulong> computeHashProgress = new ActionProgress<ulong>(bytesComputed =>
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
							(plan.ComparisonHashAlgorithmTypes ?? Array.Empty<HashAlgorithmType>()).Concat(plan.VerificationHashAlgorithmTypes ?? Array.Empty<HashAlgorithmType>())
							.Distinct()
							.ToList();

						record.Metadata.ComputedHashes = await _hashService.ComputeHashesAsync(
							record.Item.Content,
							record.Item.RelativeFilePath,
							allAlgorithms,
							computeHashProgress,
							throttler,
							cancellationToken
						);

						await throttler.WaitAsync();

						// ------------------------------------------------------------
						// Commit: resolve path → move file → write sidecar
						// ------------------------------------------------------------
						string? strongHash = GetStrongestHash(record.Metadata.ComputedHashes);
						string relativeDir = Path.GetDirectoryName(record.Item.RelativeFilePath) ?? string.Empty;
						TargetPathResolveRequest targetPathResolveRequest = new(
							DestinationRoot: plan.Destination,
							RelativeDirectoryPath: relativeDir,
							FileName: record.Item.FileName,
							CreateFileDate: createFileDate,
							StrongHash: strongHash,
							ItemId: record.Item.Id,
							OutputStructureStrategy: plan.OutputStructureStrategy,
							CustomPattern: plan.CustomOutputPattern,
							SourceDetails: record.SourceDetails
						);
						string intendedPath = _targetPathResolver.Resolve(targetPathResolveRequest);

						CollisionResult? collisionResult = null;

						if (File.Exists(intendedPath))
						{
							// collision
							CollisionResolveRequest collisionRequest = new()
							{
								SourcePath = tempFile.FullName,
								IntendedTargetPath = intendedPath,

								RelativeFilePath = record.Item.RelativeFilePath,
								CreateFileDate = createFileDate,
								ItemId = record.Item.Id,

								StrongHash = strongHash,
								ComputedHashes = record.Metadata.ComputedHashes,
								SourceDetails = record.SourceDetails,

								Strategy = plan.CollisionStrategy,

								ComparisonType = plan.CollisionComparisonType,

								RenameStrategy = plan.RenameStrategy,
								CustomRenamePattern = plan.CustomOutputCollisionPattern,

								ComparisonHashAlgorithmTypes = plan.ComparisonHashAlgorithmTypes ?? Array.Empty<HashAlgorithmType>(),
							};

							collisionResult = await _collisionResolver.ResolveAsync(collisionRequest, throttler, cancellationToken);

							switch (collisionResult.Action)
							{
								case CollisionResolutionAction.Skip:
									record.Status = BackupItemStatus.Skipped;
									TempDirectoryHelper.CleanupTempFiles(tempFile?.FullName, null);
									_currentProgress = _currentProgress with
									{
										FilesSkipped = _currentProgress.FilesSkipped + 1,
									};
									progress?.Report(_currentProgress);
									await throttler.WaitAsync();
									continue;

								case CollisionResolutionAction.Move:
								case CollisionResolutionAction.Overwrite:
									break;
							}
						}

						string targetPath = collisionResult?.TargetPath ?? intendedPath;
						bool overwrite = collisionResult?.Action == CollisionResolutionAction.Overwrite;

						string? targetDir = Path.GetDirectoryName(targetPath);
						if (!string.IsNullOrEmpty(targetDir))
						{
							Directory.CreateDirectory(targetDir);
						}

						// We know that record.Item.Content is IMoveableContent because it was created by the BackupScanner which always creates items with moveable content.
						IMoveableContent moveableContent = (IMoveableContent)record.Item.Content;
						IContent movedContent = moveableContent.MoveTo(targetPath, overwrite: overwrite);
						record.Item.ReplaceContentProvider(movedContent);
						record.DestinationPath = targetPath;

						if (plan.SidecarFormat != SidecarFormat.None)
						{
							SidecarRequest sidecarRequest = new()
							{
								Format = plan.SidecarFormat,
								SourceType = plan.SourceType,
								SourceId = record.Item.Id,
								SourceFileName = record.Item.FileName,
								SourceFullPath = record.Item.SourcePath,
								MediaTakenDateTime = record.Metadata.MediaTakenDateTime,
								AuthoredDateTime = record.Metadata.AuthoredDateTime,
								CreateDateTime = record.Metadata.CreatedDateTime,
								LastWriteDateTime = record.Metadata.ModifiedDateTime,
								LastAccessDateTime = record.Metadata.AccessedDateTime,
								BackupStartDateTime = backupStartTime,
								SourceRelativeFilePath = record.Item.RelativeFilePath,
								SanitizedSourceRelativeFilePath = record.Item.RelativeFilePath?.Replace(':', '_'),
								TargetRelativeFilePath = Path.GetRelativePath(plan.Destination, targetPath),
								Hashes = record.Metadata.ComputedHashes,
								SourceDetails = record.SourceDetails,
							};

							await _sidecarService.WriteAsync(targetPath, sidecarRequest, cancellationToken);
						}

						if (plan.PostWriteVerification == PostWriteVerificationType.Hash)
						{
							// Guard repeated because flow analysis does not track null-state across nested if blocks.
							if (record.Item.RelativeFilePath == null) throw new InvalidOperationException("Item RelativeFilePath is null.");

							if (plan.VerificationHashAlgorithmTypes == null || plan.VerificationHashAlgorithmTypes.Count == 0)
							{
								throw new InvalidOperationException("VerificationHashAlgorithmTypes must be specified for hash-based post-write verification.");
							}

							Dictionary<HashType, string> verifyHashes = await _hashService.ComputeHashesAsync(
								record.Item.Content,
								record.Item.RelativeFilePath,
								plan.VerificationHashAlgorithmTypes,
								null,
								throttler,
								cancellationToken
							);

							foreach (KeyValuePair<HashType, string> kvp in record.Metadata.ComputedHashes)
							{
								if (verifyHashes.TryGetValue(kvp.Key, out string? verifyValue) &&
									!string.Equals(kvp.Value, verifyValue, StringComparison.OrdinalIgnoreCase))
								{
									throw new InvalidOperationException(
										$"Post-write verification failed for '{record.Item.SourcePath}': " +
										$"{kvp.Key} hash mismatch."
									);
								}
							}
						}

						record.Status = BackupItemStatus.Succeeded;

						_currentProgress = _currentProgress with
						{
							FilesSucceeded = _currentProgress.FilesSucceeded + 1,
							BytesProcessed = _currentProgress.BytesProcessed + (long)record.Item.Content.Length,
						};
						progress?.Report(_currentProgress);
					}
					catch (OperationCanceledException)
					{
						// User cancelled (Ctrl+C). Clean up the in-progress temp file —
						// this is expected shutdown, not a failure. Leaving orphaned
						// temp files would clutter the .tmp directory unnecessarily.
						TempDirectoryHelper.CleanupTempFiles(tempFile?.FullName, null);
						throw;
					}
					catch (Exception ex)
					{
						record.Status = BackupItemStatus.Failed;
						_logger.LogError(ex, "Item failed: {Path}", record.Item.SourcePath);
						if (plan.StopOnError) throw;
					}

					await throttler.WaitAsync();
				}
			}
			finally
			{
				// We need to force save the session state here to capture any progress made on items in case of cancellation or unhandled exceptions. This ensures that when the user resumes, they won't lose all progress since the last save point.
				await sessionState.SaveAsync(repository.GetAll(), sessionKey, CancellationToken.None);

				// Do this even when exception or cancel.
				// Do not let cleanup errors mask original failure.
				if (sessionTempDir.Exists)
				{
					try
					{
						bool removed = TempDirectoryHelper.CleanupSessionTempDirectory(sessionTempDir);
						if (!removed)
						{
							_logger.LogDebug("Session temp directory not empty, kept: {sessionTempDir}", sessionTempDir.FullName);
						}
					}
					catch (Exception ex)
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



			IReadOnlyList<BackupResultItem> itemResults = BuildItemResults(allRecords);
			bool anyFailed = itemResults.Any(r => r.State == BackupResultItemState.Failed);

			BackupResult result = new()
			{
				Name = plan.Name,
				State = anyFailed ? BackupResultState.Failed : BackupResultState.Completed,
				ItemResults = itemResults,
			};

			// ------------------------------------------------------------
			// 8b. Write backup catalog
			// ------------------------------------------------------------

			if (plan.BackupIndexType == BackupIndexType.Json)
			{
				await _backupIndexWriter.WriteAsync(plan.Destination, sessionKey.SessionId, allRecords, plan, result, cancellationToken);
			}

			// ------------------------------------------------------------
			// 9. Return BackupResult
			// ------------------------------------------------------------

			return result;
		}
		catch (OperationCanceledException)
		{
			_logger.LogInformation("Backup cancelled by user.");

			_currentProgress = _currentProgress with
			{
				CurrentPhase = BackupProgressPhase.Completed,
			};
			progress?.Report(_currentProgress);

			BackupResult result = new()
			{
				Name = plan.Name,
				State = BackupResultState.Cancelled,
				ItemResults = BuildItemResults(repository.GetAll()),
			};

			// Write catalog even when cancelled — shows all items with their current status.
			if (plan.BackupIndexType == BackupIndexType.Json)
			{
				await _backupIndexWriter.WriteAsync(
					plan.Destination,
					sessionKey.SessionId,
					repository.GetAll(),
					plan,
					result,
					CancellationToken.None
				);
			}

			return result;
		}
	}

	private static IReadOnlyList<BackupResultItem> BuildItemResults(IReadOnlyList<BackupRecord> records)
	{
		List<BackupResultItem> itemResults = new(records.Count);

		foreach (BackupRecord record in records)
		{
			itemResults.Add(new BackupResultItem
			{
				Id = record.Item.Id,
				SourcePath = record.Item.SourcePath,
				DestinationPath = record.DestinationPath,
				Length = (long)record.Item.Content.Length,
				State = MapItemState(record.Status),
			});
		}

		return itemResults.AsReadOnly();
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

		foreach (BackupRecord record in records)
		{
			switch (record.Status)
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

	private static BackupResult BuildDryRunResult(
		IBackupRecordRepository repository,
		BackupProgress currentProgress,
		IProgress<BackupProgress>? progress,
		BackupPlan plan
	)
	{
		IReadOnlyList<BackupRecord> allRecords = repository.GetAll();

		currentProgress = currentProgress with
		{
			CurrentPhase = BackupProgressPhase.Completed,
			FilesSucceeded = allRecords.Count(r => r.Status == BackupItemStatus.Succeeded),
			FilesSkipped = allRecords.Count(r => r.Status == BackupItemStatus.Skipped),
			FilesFailed = allRecords.Count(r => r.Status == BackupItemStatus.Failed),
		};
		progress?.Report(currentProgress);

		IReadOnlyList<BackupResultItem> itemResults = BuildItemResults(allRecords);
		bool anyFailed = itemResults.Any(r => r.State == BackupResultItemState.Failed);

		return new BackupResult
		{
			Name = plan.Name,
			State = anyFailed ? BackupResultState.Failed : BackupResultState.Completed,
			IsDryRun = true,
			ItemResults = itemResults,
		};
	}

	private static string? GetStrongestHash(Dictionary<HashType, string>? computedHashes)
	{
		if (computedHashes == null || computedHashes.Count == 0)
		{
			return null;
		}

		HashType[] priority =
		[
			HashType.BLAKE3_512,
			HashType.SHA3_512_FIPS202,
			HashType.SHA3_512_KECCAK,
			HashType.BLAKE3_256,
			HashType.SHA2_512,
			HashType.SHA3_256_FIPS202,
			HashType.SHA3_256_KECCAK,
			HashType.SHA2_256,
			HashType.MD5_128,
		];

		foreach (HashType type in priority)
		{
			if (computedHashes.TryGetValue(type, out string? hash))
			{
				return hash;
			}
		}

		return computedHashes.Values.FirstOrDefault();
	}

	private static IBackupDriveInfo? MatchDrive(IReadOnlyList<IBackupDriveInfo> drives, string internalUri)
	{
		foreach (IBackupDriveInfo drive in drives)
		{
			Guard.RequireNonNull(drive);

			if (internalUri.StartsWith(drive.RootPath, StringComparison.OrdinalIgnoreCase))
			{
				return drive;
			}
		}

		return null;
	}
}
