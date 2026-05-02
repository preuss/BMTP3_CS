using System.Diagnostics;
using BMTP3.Core2.BackupNew.Api;
using BMTP3.Core2.BackupNew.Api.Enums;
using BMTP3.Core2.BackupNew.Api.Progress;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Api.Response;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Domain.Job;
using BMTP3.Core2.BackupNew.Domain.Repositories;
using BMTP3.Core2.BackupNew.Engine.Hashing;
using BMTP3.Core2.BackupNew.Engine.Internal;
using BMTP3.Core2.BackupNew.Engine.Orchestration;
using BMTP3.Core2.BackupNew.Engine.Staging;
using BMTP3.Core2.BackupNew.Engine.Steps.HashStep;
using BMTP3.Core2.BackupNew.Engine.Steps.InspectorStep;
using BMTP3.Core2.BackupNew.Engine.Steps.MetadataExtractionStep;
using BMTP3.Core2.BackupNew.Engine.Steps.SidecarGenerationStep;
using BMTP3.Core2.BackupNew.Engine.Steps.StagingStep;
using BMTP3.Core2.BackupNew.Engine.Steps.TimestampCorrectionStep;
using BMTP3.Core2.BackupNew.Engine.Steps.TransferStep;
using BMTP3.Core2.BackupNew.Engine.Strategies;
using BMTP3.Core2.BackupNew.Engine.Transfers;
using BMTP3.Core2.BackupNew.Engine.Traversal;
using BMTP3.Core2.BackupNew.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BMTP3.Core2.BackupNew.Engine;

/// <summary>
///     Sequential BackupEngine implementation: processes items one at a time through a linear pipeline.
///     Unlike BackupEngine (which uses bounded channels and parallel workers), BackupEngineSequentiel
///     executes each step synchronously for each item. This makes it simpler to debug and reason about,
///     though slower for large backups.
///
///     Pipeline: Scan → Buffer → Metadata → Timestamp → Hash → Transfer → Inspector → Sidecar → Persist
/// </summary>
public class BackupEngineSequentiel : IBackupEngine
{
	private readonly IBackupScanner _backupScanner;
	private readonly ICollisionResolver _collisionResolver;
	private readonly IDestinationInspector _destinationInspector;
	private readonly IFileTransfer _fileTransfer;
	private readonly IHashGenerator _hashGenerator;
	private readonly IItemHasher _itemHasher;
	private readonly ILogger<BackupEngineSequentiel> _logger;
	private readonly ILoggerFactory _loggerFactory;
	private readonly IMetadataReader _metadataReader;
	private readonly IOptions<BackupEngineOptions> _options;
	private readonly IPathGenerator _pathGenerator;
	private readonly IBackupRepository _repository;
	private readonly ISidecarGeneratorFactory _sidecarGeneratorFactory;
	private readonly IStagingDownloader _stagingDownloader;
	private readonly IJobValidator _validator;

	public BackupEngineSequentiel(
		IBackupScanner backupScanner,
		IStagingDownloader stagingDownloader,
		IMetadataReader metadataReader,
		IItemHasher itemHasher,
		IPathGenerator pathGenerator,
		ICollisionResolver collisionResolver,
		IFileTransfer fileTransfer,
		IHashGenerator hashGenerator,
		IBackupRepository repository,
		ISidecarGeneratorFactory sidecarGeneratorFactory,
		IJobValidator validator,
		IOptions<BackupEngineOptions> options,
		ILoggerFactory loggerFactory,
		IDestinationInspector destinationInspector
	)
	{
		_backupScanner = backupScanner ?? throw new ArgumentNullException(nameof(backupScanner));
		_stagingDownloader = stagingDownloader ?? throw new ArgumentNullException(nameof(stagingDownloader));
		_metadataReader = metadataReader ?? throw new ArgumentNullException(nameof(metadataReader));
		_itemHasher = itemHasher ?? throw new ArgumentNullException(nameof(itemHasher));
		_hashGenerator = hashGenerator ?? throw new ArgumentNullException(nameof(hashGenerator));
		_pathGenerator = pathGenerator ?? throw new ArgumentNullException(nameof(pathGenerator));
		_collisionResolver = collisionResolver ?? throw new ArgumentNullException(nameof(collisionResolver));
		_fileTransfer = fileTransfer ?? throw new ArgumentNullException(nameof(fileTransfer));
		_repository = repository ?? throw new ArgumentNullException(nameof(repository));
		_sidecarGeneratorFactory =
			sidecarGeneratorFactory ?? throw new ArgumentNullException(nameof(sidecarGeneratorFactory));
		_validator = validator ?? throw new ArgumentNullException(nameof(validator));
		_options = options ?? throw new ArgumentNullException(nameof(options));
		_loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
		_logger = _loggerFactory.CreateLogger<BackupEngineSequentiel>();
		_destinationInspector = destinationInspector ?? throw new ArgumentNullException(nameof(destinationInspector));
	}

	public async Task<BackupJobResult> RunAsync(BackupPlan plan, IProgress<IBackupProgress> progress, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(plan);

		progress ??= new Progress<IBackupProgress>();

		// Adapter: forwards BackupProgress snapshots to caller's IProgress<IBackupProgress>.
		IProgress<BackupProgress> stagingProgress = new Progress<BackupProgress>(p => progress.Report(p));

		// 0. Pre-flight Validation
		await _validator.ValidateAsync(plan, ct);

		// 0b. Runtime validation: disk space
		ValidateDiskSpace(plan);

		// 0c. Pre-flight: validate source and output
		ValidateSourceAndOutput(plan);

		// 0d. Initialize backup session for resume support
		Guid sessionId = Guid.NewGuid();
		BackupSessionEntity session = new()
		{
			SessionId = sessionId,
			Plan = plan,
			Records = new List<BackupResumeRecord>()
		};

		// Save initial session state (for resume support on crash/cancellation)
		if(!plan.DryRun)
		{
			await _repository.SaveAsync(session, ct).ConfigureAwait(false);
			_logger.LogInformation("Backup session {SessionId} started (sequential). Output: {OutputPath}", sessionId,
				plan.OutputPath);
		}

		// Prepare result
		ProgressTracker tracker = new();
		tracker.SetPhase(BackupPhase.Starting);

		BackupJobResult result = new()
		{
			JobName = plan.Name,
			StartTime = DateTime.UtcNow,
			Status = JobState.Ready
		};

		if(ct.IsCancellationRequested)
		{
			result.EndTime = DateTime.UtcNow;
			result.Status = JobState.Cancelled;
			result.GlobalErrors.Add("Cancelled before start");
			return result;
		}

		// Launch background reporting task
		CancellationTokenSource reportingCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		Task reportingTask = Task.Run(async () =>
		{
			try
			{
				while(!reportingCts.Token.IsCancellationRequested)
				{
					progress.Report(tracker.GetSnapshot());
					await Task.Delay(250, reportingCts.Token).ConfigureAwait(false);
				}
			}
			catch(OperationCanceledException)
			{
			}
		}, reportingCts.Token);

		// Configure Options
		BackupEngineOptions opts = _options.Value;

		result.Status = JobState.Running;
		tracker.SetPhase(BackupPhase.Traversing);

		// Instantiate Steps
		ContentBufferingItemStep bufferingStep = new(_stagingDownloader, stagingProgress, plan);
		MetadataExtractionItemStep metadataStep = new(_metadataReader, plan);
		TimestampCorrectionItemStep timestampStep = new(plan);

		HashStepContext hashStepContext = new()
		{
			HashTypes = plan.HashTypes.ToList(),
			ForceRecompute = false
		};
		HashItemStep hashStep = new(hashStepContext, _itemHasher, _loggerFactory.CreateLogger<HashItemStep>());

		TransferItemStep transferStep = new(plan, _pathGenerator, _collisionResolver, _fileTransfer, _itemHasher);
		DestinationInspectorItemStep inspectorStep = new(_destinationInspector, _hashGenerator, plan,
			_loggerFactory.CreateLogger<DestinationInspectorItemStep>());
		ISidecarGeneratorFactory sidecarFactory = _sidecarGeneratorFactory;
		SidecarGenerationItemStep sidecarStep = new(plan, sidecarFactory);

		// MTP device session handling
		IMtpDeviceSession? mtpSession = null;
		if(plan.SourceType == SourceType.MediaDevice && _backupScanner is IMtpCapableScanner mtpScanner)
		{
			mtpSession = mtpScanner.OpenSession(plan);
		}

		int itemsSinceLastSave = 0;
		const int saveInterval = 10; // Save every 10 items

		// Create pipeline with all steps
		var pipelineSteps = new[]
		{
			new PipelineStep("Buffering", FilePhase.Staging,
				(item, prog, ct) => bufferingStep.ExecuteAsync(item, prog, ct)),
			new PipelineStep("Metadata", FilePhase.Metadata,
				(item, prog, ct) => metadataStep.ExecuteAsync(item, prog, ct)),
			new PipelineStep("Timestamp", FilePhase.Metadata,
				(item, prog, ct) => timestampStep.ExecuteAsync(item, prog, ct)),
			new PipelineStep("Hashing", FilePhase.Hashing,
				(item, prog, ct) => hashStep.ExecuteAsync(item, prog, ct)),
			new PipelineStep("Planning", FilePhase.Planning,
				(item, prog, ct) => transferStep.ExecuteAsync(item, prog, ct)),
			new PipelineStep("Inspector", FilePhase.Transferring,
				(item, prog, ct) => inspectorStep.ExecuteAsync(item, prog, ct)),
			new PipelineStep("Sidecar", FilePhase.Transferring,
				(item, prog, ct) => sidecarStep.ExecuteAsync(item, prog, ct))
		};

		try
		{
			// Sequential scanner loop
			await foreach(IBackupItem item in _backupScanner.ScanAsync(plan, ct))
			{
				ct.ThrowIfCancellationRequested();

				ulong length = 0;
				if(item.Metadata.Has(MetadataKey.Length))
				{
					length = item.Metadata.Get<ulong>(MetadataKey.Length);
				}

				tracker.AddDiscovery(false, (long)length);

				// Create progress reporter for this item
				IProgress<ulong> itemProgress = new Progress<ulong>(bytes =>
				{
					tracker.UpdateItemBytes(item.Id, bytes);
				});

				try
				{
					// Extract item metadata for phase tracking
					string sourcePath = item.Metadata.Get<string>(MetadataKey.SourceFullPath) ?? "";
					string fileName = item.Metadata.Get<string>(MetadataKey.SourceFileName) ?? "";
					string relativePath = item.Metadata.Get<string>(MetadataKey.SourceRelativePath) ?? "";

					// After metadata extraction, length should be set
					if(!item.Metadata.Has(MetadataKey.Length) && length == 0)
					{
						if(item.Metadata.Has(MetadataKey.Length))
						{
							length = item.Metadata.Get<ulong>(MetadataKey.Length);
						}
					}

					// Process item through pipeline
					var pipeline = new SequentialItemPipeline(tracker, pipelineSteps, itemProgress);
					await pipeline.ProcessItemAsync(item, sourcePath, fileName, relativePath, length, ct)
						.ConfigureAwait(false);
				}
				catch(OperationCanceledException)
				{
					throw;
				}

				// Persist item state for resume support (skip in DryRun)
				if(!plan.DryRun)
				{
					await _repository.PersistItemStateAsync(item, ct).ConfigureAwait(false);
					itemsSinceLastSave++;

					// Periodic save to avoid losing progress on crash
					if(itemsSinceLastSave >= saveInterval)
					{
						await _repository.SaveAsync(session, ct).ConfigureAwait(false);
						itemsSinceLastSave = 0;
					}
				}
			}

			// Set phase to Transferring after scanning completes
			tracker.SetPhase(BackupPhase.Transferring);
		}
		catch(OperationCanceledException)
		{
			// Cancellation requested - will be handled below
		}
		catch(Exception ex)
		{
			// Convert unexpected exceptions into a failed BackupJobResult
			tracker.SetPhase(BackupPhase.Completed);

			result.EndTime = DateTime.UtcNow;
			result.Status = JobState.Failed;
			result.GlobalErrors.Add($"Pipeline crashed: {ex.Message}");
			result.GlobalErrors.Add(ex.ToString());

			// Ensure reporting task is stopped
			try
			{
				reportingCts.Cancel();
				await reportingTask.ConfigureAwait(false);
			}
			catch
			{
				// ignored
			}

			// Cleanup MTP session on pipeline crash
			if(mtpSession != null)
			{
				try
				{
					mtpSession.Dispose();
				}
				catch
				{
					/* best effort */
				}
			}

			return result;
		}
		finally
		{
			// Cleanup MTP session after pipeline completes (success or cancellation)
			if(mtpSession != null)
			{
				try
				{
					mtpSession.Dispose();
				}
				catch
				{
					/* best effort */
				}
			}

			// Stop the reporting task
			try
			{
				reportingCts.Cancel();
				await reportingTask.ConfigureAwait(false);
			}
			catch
			{
				// ignored
			}
		}

		// Compile final result
		CompileResult(result, tracker, ct);
		progress.Report(tracker.GetSnapshot());

		// Final save of session state (skip in DryRun)
		if(!plan.DryRun)
		{
			await _repository.SaveAsync(session, ct).ConfigureAwait(false);
			_logger.LogInformation("Backup session {SessionId} completed (sequential). Files: {Copied}/{Total}, Status: {Status}",
				sessionId, result.FilesCopied, result.TotalFilesScanned, result.Status);
		}

		return result;
	}

	private void ValidateDiskSpace(BackupPlan plan)
	{
		DirectoryInfo outputDir = new(plan.OutputPath);
		if(!outputDir.Exists)
		{
			outputDir.Create();
		}

		const long oneMb = 1024 * 1024;
		const long oneGb = 1024 * oneMb;

		long freeSpace = GetFreeSpace(outputDir.FullName);
		long freeSpaceMb = freeSpace / oneMb;
		long freeSpaceGb = freeSpace / oneGb;

		// Thresholds:
		// > 10GB: no log
		// 1GB - 10GB: info
		// 100MB - 1GB: warning
		// < 100MB: exception

		const long oneHundredMb = 100 * oneMb;
		const long tenGb = 10 * oneGb;

		if(freeSpace <= oneHundredMb)
		{
			// < 100MB: exception
			throw new IOException(
				$"Insufficient disk space on output drive '{outputDir.Root}'. Available: {freeSpaceMb} MB.");
		}

		if(freeSpace <= oneGb)
		{
			// 100MB - 1GB: warning
			_logger.LogWarning("Low disk space on output drive '{Drive}': {FreeSpace} MB. Backup may fail.",
				outputDir.Root, freeSpaceMb);
		}
		else if(freeSpace <= tenGb)
		{
			// 1GB - 10GB: info
			_logger.LogInformation("Disk space on output drive '{Drive}': {FreeSpace} GB available.", outputDir.Root,
				freeSpaceGb);
		}
	}

	private void ValidateSourceAndOutput(BackupPlan plan)
	{
		if(plan.SourceType == SourceType.FileSystem)
		{
			if(!Directory.Exists(plan.SourcePath))
			{
				throw new DirectoryNotFoundException($"Source path not found: {plan.SourcePath}");
			}

			// Verify read access by attempting to enumerate one entry – never write to the source.
			try
			{
				Directory.EnumerateFileSystemEntries(plan.SourcePath).FirstOrDefault();
			}
			catch(Exception ex)
			{
				throw new UnauthorizedAccessException($"No read access to source: {plan.SourcePath}", ex);
			}
		}

		DirectoryInfo outputDir = new(plan.OutputPath);
		if(!outputDir.Exists)
		{
			try
			{
				outputDir.Create();
			}
			catch(Exception ex)
			{
				throw new IOException($"Cannot create output directory: {plan.OutputPath}", ex);
			}
		}

		try
		{
			string testFile = Path.Combine(plan.OutputPath, Path.GetRandomFileName());
			File.WriteAllText(testFile, "test");
			File.Delete(testFile);
		}
		catch(Exception ex)
		{
			throw new UnauthorizedAccessException($"No write access to output: {plan.OutputPath}", ex);
		}
	}

	private long GetFreeSpace(string path)
	{
		try
		{
			string root = Path.GetPathRoot(path) ?? path;
			DriveInfo drive = new(root);
			return drive.AvailableFreeSpace;
		}
		catch(Exception)
		{
			// If we can't determine free space, assume minimal to trigger validation failure
			// This is safer than returning MaxValue which could skip the check
			return 0;
		}
	}

	private void CompileResult(BackupJobResult result, ProgressTracker tracker, CancellationToken ct)
	{
		// Decide final status based on cancellation and per-item failures
		BackupProgress finalSnap = tracker.GetSnapshot();

		result.EndTime = DateTime.UtcNow;

		if(ct.IsCancellationRequested)
		{
			result.Status = JobState.Cancelled;
			tracker.SetPhase(BackupPhase.Cancelled);
		}
		else if(finalSnap.FilesFailed > 0)
		{
			result.Status = JobState.Failed;
			tracker.SetPhase(BackupPhase.Completed);
			result.GlobalErrors.Add($"{finalSnap.FilesFailed} file(s) failed during the run.");
		}
		else
		{
			result.Status = JobState.Completed;
			tracker.SetPhase(BackupPhase.Completed);
		}

		// Populate summary fields from tracker snapshot
		result.FilesCopied = finalSnap.FilesSucceeded;
		result.FilesFailed = finalSnap.FilesFailed;
		result.FilesSkipped = finalSnap.FilesSkipped;
		result.TotalFilesScanned = finalSnap.FilesDiscovered;
		result.TotalBytesCopied = finalSnap.BytesProcessed;
	}
}
