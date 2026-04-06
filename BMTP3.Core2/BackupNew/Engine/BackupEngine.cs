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
using System.Threading.Channels;

namespace BMTP3.Core2.BackupNew.Engine;
/// <summary>
/// Skeleton BackupEngine
/// </summary>
public class BackupEngine : IBackupEngine
{
	private readonly IBackupScanner _backupScanner;
	private readonly IStagingDownloader _stagingDownloader;
	private readonly IMetadataReader _metadataReader;
	private readonly IItemHasher _itemHasher;
	private readonly IHashGenerator _hashGenerator;
	private readonly IPathGenerator _pathGenerator;
	private readonly ICollisionResolver _collisionResolver;
	private readonly IFileTransfer _fileTransfer;
	private readonly IBackupRepository _repository;
	private readonly ISidecarGeneratorFactory _sidecarGeneratorFactory;
	private readonly IJobValidator _validator;
	private readonly IOptions<BackupEngineOptions> _options;
	private readonly ILoggerFactory _loggerFactory;
	private readonly ILogger<BackupEngine> _logger;
	private readonly IDestinationInspector _destinationInspector;

	public BackupEngine(
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
		_sidecarGeneratorFactory = sidecarGeneratorFactory ?? throw new ArgumentNullException(nameof(sidecarGeneratorFactory));
		_validator = validator ?? throw new ArgumentNullException(nameof(validator));
		_options = options ?? throw new ArgumentNullException(nameof(options));
		_loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
		_logger = _loggerFactory.CreateLogger<BackupEngine>();
		_destinationInspector = destinationInspector ?? throw new ArgumentNullException(nameof(destinationInspector));
	}

	public async Task<BackupJobResult> RunAsync(BackupPlan plan, IProgress<IBackupProgress> progress, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(plan);

		// TODO: I am trying to make this not nullable, but for now just in case, use null-coalescing assignment to ensure it's not null. We can remove this once we are sure all callers provide a non-null progress instance.
		progress ??= new Progress<IBackupProgress>();

		// Adapter: forwards BackupProgress snapshots to the caller's IProgress<IBackupProgress>.
		// ContentBufferingItemStep (and StagingDownloader) report per-file staging updates using
		// BackupProgress. We bridge those into the same outer progress channel so callers see
		// staging activity without needing to know about the internal type.
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
			_logger.LogInformation("Backup session {SessionId} started. Output: {OutputPath}", sessionId, plan.OutputPath);
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

		// reportingTask should be cancellable independently so we can stop it when the pipeline completes
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
			} catch(OperationCanceledException) { }
		}, reportingCts.Token);

		// Configure Options
		BackupEngineOptions opts = _options.Value;

		// Allow single-threaded debug mode when:
		// - BackupEngineOptions.DebugSingleThreaded == true OR
		// - a debugger is attached (convenient during development)
		bool debugSingleThread = (opts.DebugSingleThreaded) || System.Diagnostics.Debugger.IsAttached;

		int degreeOfParallelism = debugSingleThread
			? 1
			: (opts.DegreeOfParallelism > 0
				? opts.DegreeOfParallelism
				: Math.Max(1, Environment.ProcessorCount / 2));

		// Define Channels
		Channel<IBackupItem> scanChannel = Channel.CreateBounded<IBackupItem>(new BoundedChannelOptions(opts.ScanChannelCapacity) { SingleWriter = false, SingleReader = true });
		//var convertChannel = Channel.CreateBounded<IBackupItem>(new BoundedChannelOptions(opts.ConvertChannelCapacity) { SingleWriter = false, SingleReader = true });
		Channel<IBackupItem> bufferingChannel = Channel.CreateBounded<IBackupItem>(new BoundedChannelOptions(opts.StagingChannelCapacity) { SingleWriter = false, SingleReader = true });
		Channel<IBackupItem> metadataChannel = Channel.CreateBounded<IBackupItem>(new BoundedChannelOptions(opts.ProcessingChannelCapacity) { SingleWriter = false, SingleReader = true });
		Channel<IBackupItem> timestampChannel = Channel.CreateBounded<IBackupItem>(new BoundedChannelOptions(opts.ProcessingChannelCapacity) { SingleWriter = false, SingleReader = true });
		Channel<IBackupItem> hashChannel = Channel.CreateBounded<IBackupItem>(new BoundedChannelOptions(opts.ProcessingChannelCapacity) { SingleWriter = false, SingleReader = true });
		Channel<IBackupItem> inspectorChannel = Channel.CreateBounded<IBackupItem>(new BoundedChannelOptions(opts.ProcessingChannelCapacity) { SingleWriter = false, SingleReader = true });
		Channel<IBackupItem> transferChannel = Channel.CreateBounded<IBackupItem>(new BoundedChannelOptions(opts.ProcessingChannelCapacity) { SingleWriter = false, SingleReader = true });
		Channel<IBackupItem> sidecarChannel = Channel.CreateBounded<IBackupItem>(new BoundedChannelOptions(opts.ProcessingChannelCapacity) { SingleWriter = false, SingleReader = true });
		Channel<IBackupItem> persistenceChannel = Channel.CreateBounded<IBackupItem>(new BoundedChannelOptions(opts.ProcessingChannelCapacity) { SingleWriter = false, SingleReader = true });

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
		// Destination Inspector
		DestinationInspectorItemStep inspectorStep = new(_destinationInspector, _hashGenerator, plan, _loggerFactory.CreateLogger<DestinationInspectorItemStep>());
		ISidecarGeneratorFactory sidecarFactory = _sidecarGeneratorFactory;
		SidecarGenerationItemStep sidecarStep = new(plan, sidecarFactory);

		// Source Reading MUST be serial (1 thread) to prevent MTP timeouts and IO thrashing.
		// We ignore degreeOfParallelism for this specific step.
		ContentBufferingPipelineStage bufferingPool = new(_loggerFactory.CreateLogger<ContentBufferingPipelineStage>(), 1, plan, tracker, bufferingStep);

		MetadataExtractionPipelineStage metadataPool = new(_loggerFactory.CreateLogger<MetadataExtractionPipelineStage>(), degreeOfParallelism, plan, tracker, metadataStep);
		TimestampCorrectionPipelineStage timestampPool = new(_loggerFactory.CreateLogger<TimestampCorrectionPipelineStage>(), degreeOfParallelism, plan, tracker, timestampStep);
		HashPipelineStage hashPool = new(_loggerFactory.CreateLogger<HashPipelineStage>(), degreeOfParallelism, hashStepContext, tracker, hashStep);
		DestinationInspectorPipelineStage inspectorPool = new(_loggerFactory.CreateLogger<DestinationInspectorPipelineStage>(), degreeOfParallelism, plan, tracker, inspectorStep);
		TransferPipelineStage transferPool = new(_loggerFactory.CreateLogger<TransferPipelineStage>(), degreeOfParallelism, plan, tracker, transferStep);
		SidecarGenerationPipelineStage sidecarPool = new(_loggerFactory.CreateLogger<SidecarGenerationPipelineStage>(), degreeOfParallelism, plan, tracker, sidecarStep);


		// Start Pipeline Tasks
		// For MTP sources, open a device session before the pipeline starts and dispose it
		// only after bufferingTask completes. This guarantees the device stays connected
		// for the full duration that MediaFileContent.OpenRead() may be called by
		// ContentBufferingPipelineStage, eliminating the race condition where
		// device.Disconnect() could run before all items had been staged.
		IMtpDeviceSession? mtpSession = null;
		if(plan.SourceType == SourceType.MediaDevice && _backupScanner is IMtpCapableScanner mtpScanner)
		{
			mtpSession = mtpScanner.OpenSession(plan);
		}

		Task producerTask = Task.Run(async () =>
		{
			try
			{
				await foreach(IBackupItem item in _backupScanner.ScanAsync(plan, ct))
				{
					ct.ThrowIfCancellationRequested();

					ulong length = 0;
					if(item.Metadata.Has(MetadataKey.Length))
					{
						length = item.Metadata.Get<ulong>(MetadataKey.Length);
					}
					tracker.AddDiscovery(false, (long)length);
					await scanChannel.Writer.WriteAsync(item, ct);
				}
			} catch(OperationCanceledException) { } finally
			{
				scanChannel.Writer.Complete();
			}
		}, ct);

		Task bufferingTask = Task.Run(() => bufferingPool.RunAsync(scanChannel.Reader, bufferingChannel.Writer, ct), ct);

		// Dispose MTP session after all file content has been read from the device.
		// bufferingTask completes only after ContentBufferingPipelineStage has staged every item,
		// so at this point it is safe to disconnect. Using ContinueWith with ExecuteSynchronously
		// ensures the dispose runs immediately when bufferingTask finishes, regardless of the
		// downstream pipeline state.
		if(mtpSession != null)
		{
			_ = bufferingTask.ContinueWith(
				_ => mtpSession.Dispose(),
				CancellationToken.None,
				TaskContinuationOptions.ExecuteSynchronously,
				TaskScheduler.Default);
		}
		Task metadataTask = Task.Run(() => metadataPool.RunAsync(bufferingChannel.Reader, metadataChannel.Writer, ct), ct);
		Task timestampTask = Task.Run(() => timestampPool.RunAsync(metadataChannel.Reader, timestampChannel.Writer, ct), ct);
		Task hashTask = Task.Run(() => hashPool.RunAsync(timestampChannel.Reader, hashChannel.Writer, ct), ct);
		Task transferTask = Task.Run(() => transferPool.RunAsync(hashChannel.Reader, transferChannel.Writer, ct), ct);
		Task inspectorTask = Task.Run(() => inspectorPool.RunAsync(transferChannel.Reader, inspectorChannel.Writer, ct), ct);
		Task sidecarTask = Task.Run(() => sidecarPool.RunAsync(inspectorChannel.Reader, persistenceChannel.Writer, ct), ct);

		Task completionTask = Task.Run(async () =>
		{
			tracker.SetPhase(BackupPhase.Transferring);

			int itemsSinceLastSave = 0;
			const int saveInterval = 10; // Save every 10 items

			try
			{
				await foreach(IBackupItem item in persistenceChannel.Reader.ReadAllAsync(ct))
				{
					ct.ThrowIfCancellationRequested();

					ulong size = item.Metadata.Get<ulong>(MetadataKey.Length);
					tracker.CompleteItem(
						item.Id,
						item.ResultState,
						(long)size
					);

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
			} catch(OperationCanceledException) { }

		}, ct);

		try
		{
			await Task.WhenAll(
				producerTask,
				bufferingTask,
				metadataTask,
				timestampTask,
				hashTask,
				inspectorTask,
				transferTask,
				sidecarTask,
				completionTask
			).ConfigureAwait(false);
		} catch(OperationCanceledException)
		{
			// cancellation requested - mark as cancelled below
		} catch(Exception ex)
		{
			// Convert unexpected pipeline exceptions into a failed BackupJobResult
			tracker.SetPhase(BackupPhase.Completed);

			result.EndTime = DateTime.UtcNow;
			result.Status = JobState.Failed;
			result.GlobalErrors.Add($"Pipeline crashed: {ex.Message}");
			result.GlobalErrors.Add(ex.ToString());

			// Ensure reporting task is stopped and observed
			try
			{
				reportingCts.Cancel();
				await reportingTask.ConfigureAwait(false);
			} catch
			{
				// ignored
			}

			return result;
		}

		// Pipeline completed normally - stop the reporting task
		try
		{
			reportingCts.Cancel();
			await reportingTask.ConfigureAwait(false);
		} catch
		{
			// ignored
		}

		// Decide final status based on cancellation and per-item failures collected by the tracker.
		// Pipeline stages convert exceptions into per-item failures (they do not throw),
		// so we must inspect the progress snapshot to determine overall job outcome.
		BackupProgress finalSnap = tracker.GetSnapshot();

		result.EndTime = DateTime.UtcNow;

		if(ct.IsCancellationRequested)
		{
			result.Status = JobState.Cancelled;
			tracker.SetPhase(BackupPhase.Cancelled);
		} else if(finalSnap.FilesFailed > 0)
		{
			// Mark job as failed when any file failed. Include a short summary in GlobalErrors.
			result.Status = JobState.Failed;
			tracker.SetPhase(BackupPhase.Completed);
			result.GlobalErrors.Add($"{finalSnap.FilesFailed} file(s) failed during the run.");
		} else
		{
			result.Status = JobState.Completed;
			tracker.SetPhase(BackupPhase.Completed);
		}

		progress.Report(tracker.GetSnapshot());

		// Populate summary fields from tracker snapshot
		result.FilesCopied = finalSnap.FilesSucceeded;
		result.FilesFailed = finalSnap.FilesFailed;
		result.FilesSkipped = finalSnap.FilesSkipped;
		result.TotalFilesScanned = finalSnap.FilesDiscovered;
		result.TotalBytesCopied = finalSnap.BytesProcessed;

		// Final save of session state (skip in DryRun)
		if(!plan.DryRun)
		{
			await _repository.SaveAsync(session, ct).ConfigureAwait(false);
			_logger.LogInformation("Backup session {SessionId} completed. Files: {Copied}/{Total}, Status: {Status}", 
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
			throw new IOException($"Insufficient disk space on output drive '{outputDir.Root}'. Available: {freeSpaceMb} MB.");
		}

		if(freeSpace <= oneGb)
		{
			// 100MB - 1GB: warning
			_logger.LogWarning("Low disk space on output drive '{Drive}': {FreeSpace} MB. Backup may fail.", outputDir.Root, freeSpaceMb);
		} else if(freeSpace <= tenGb)
		{
			// 1GB - 10GB: info
			_logger.LogInformation("Disk space on output drive '{Drive}': {FreeSpace} GB available.", outputDir.Root, freeSpaceGb);
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

			try
			{
				string testFile = Path.Combine(plan.SourcePath, Path.GetRandomFileName());
				File.WriteAllText(testFile, "test");
				File.Delete(testFile);
			} catch(Exception ex)
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
			} catch(Exception ex)
			{
				throw new IOException($"Cannot create output directory: {plan.OutputPath}", ex);
			}
		}

		try
		{
			string testFile = Path.Combine(plan.OutputPath, Path.GetRandomFileName());
			File.WriteAllText(testFile, "test");
			File.Delete(testFile);
		} catch(Exception ex)
		{
			throw new UnauthorizedAccessException($"No write access to output: {plan.OutputPath}", ex);
		}
	}

	private static long GetFreeSpace(string path)
	{
		try
		{
			string root = Path.GetPathRoot(path) ?? path;
			DriveInfo drive = new(root);
			return drive.AvailableFreeSpace;
		} catch
		{
			return long.MaxValue;
		}
	}
}
