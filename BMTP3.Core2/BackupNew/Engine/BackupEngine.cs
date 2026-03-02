using BMTP3.Core2.BackupNew.Api;
using BMTP3.Core2.BackupNew.Api.Enums;
using BMTP3.Core2.BackupNew.Api.Progress;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Response;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Domain.Job;
using BMTP3.Core2.BackupNew.Engine.Hashing;
using BMTP3.Core2.BackupNew.Engine.Internal;
using BMTP3.Core2.BackupNew.Engine.Orchestration;
using BMTP3.Core2.BackupNew.Engine.Staging;
using BMTP3.Core2.BackupNew.Engine.Steps.HashStep;
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
	private readonly IPathGenerator _pathGenerator;
	private readonly ICollisionResolver _collisionResolver;
	private readonly IFileTransfer _fileTransfer;
	private readonly IBackupRepository _repository;
	private readonly ISidecarGenerator _sidecarGenerator;
	private readonly IJobValidator _validator;
	private readonly IOptions<BackupEngineOptions> _options;
	private readonly ILoggerFactory _loggerFactory;

	public BackupEngine(
		IBackupScanner backupScanner,
		IStagingDownloader stagingDownloader,
		IMetadataReader metadataReader,
		IItemHasher itemHasher,
		IPathGenerator pathGenerator,
		ICollisionResolver collisionResolver,
		IFileTransfer fileTransfer,
		IBackupRepository repository,
		ISidecarGenerator sidecarGenerator,
		IJobValidator validator,
		IOptions<BackupEngineOptions> options,
		ILoggerFactory loggerFactory
	)
	{
		_backupScanner = backupScanner ?? throw new ArgumentNullException(nameof(backupScanner));
		_stagingDownloader = stagingDownloader ?? throw new ArgumentNullException(nameof(stagingDownloader));
		_metadataReader = metadataReader ?? throw new ArgumentNullException(nameof(metadataReader));
		_itemHasher = itemHasher ?? throw new ArgumentNullException(nameof(itemHasher));
		_pathGenerator = pathGenerator ?? throw new ArgumentNullException(nameof(pathGenerator));
		_collisionResolver = collisionResolver ?? throw new ArgumentNullException(nameof(collisionResolver));
		_fileTransfer = fileTransfer ?? throw new ArgumentNullException(nameof(fileTransfer));
		_repository = repository ?? throw new ArgumentNullException(nameof(repository));
		_sidecarGenerator = sidecarGenerator ?? throw new ArgumentNullException(nameof(sidecarGenerator));
		_validator = validator ?? throw new ArgumentNullException(nameof(validator));
		_options = options ?? throw new ArgumentNullException(nameof(options));
		_loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
	}

	public async Task<BackupJobResult> RunAsync(BackupPlan plan, IProgress<IBackupProgress> progress, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(plan);
		progress ??= new Progress<IBackupProgress>();

		// 0. Pre-flight Validation
		await _validator.ValidateAsync(plan, ct);

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

		Task reportingTask = Task.Run(async () =>
		{
			while(!ct.IsCancellationRequested)
			{
				progress.Report(tracker.GetSnapshot());
				await Task.Delay(250, ct);
			}
		}, ct);

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
		Channel<IBackupItem> transferChannel = Channel.CreateBounded<IBackupItem>(new BoundedChannelOptions(opts.ProcessingChannelCapacity) { SingleWriter = false, SingleReader = true });
		Channel<IBackupItem> sidecarChannel = Channel.CreateBounded<IBackupItem>(new BoundedChannelOptions(opts.ProcessingChannelCapacity) { SingleWriter = false, SingleReader = true });
		Channel<IBackupItem> persistenceChannel = Channel.CreateBounded<IBackupItem>(new BoundedChannelOptions(opts.ProcessingChannelCapacity) { SingleWriter = false, SingleReader = true });

		result.Status = JobState.Running;
		tracker.SetPhase(BackupPhase.Traversing);

		// Instantiate Steps
		ContentBufferingItemStep bufferingStep = new(_stagingDownloader, progress);
		MetadataExtractionItemStep metadataStep = new(_metadataReader, plan);
		TimestampCorrectionItemStep timestampStep = new(plan);

		HashStepContext hashStepContext = new()
		{
			HashTypes = new List<HashType> { HashType.BLAKE3_512 },
			ForceRecompute = false
		};
		HashItemStep hashStep = new(hashStepContext, _itemHasher, _loggerFactory.CreateLogger<HashItemStep>());

		TransferItemStep transferStep = new(plan, _pathGenerator, _collisionResolver, _fileTransfer, _itemHasher);
		SidecarGenerationItemStep sidecarStep = new(plan, _sidecarGenerator);

		// Source Reading MUST be serial (1 thread) to prevent MTP timeouts and IO thrashing.
		// We ignore degreeOfParallelism for this specific step.
		ContentBufferingPipelineStage bufferingPool = new(_loggerFactory.CreateLogger<ContentBufferingPipelineStage>(), 1, plan, tracker, bufferingStep);

		MetadataExtractionPipelineStage metadataPool = new(_loggerFactory.CreateLogger<MetadataExtractionPipelineStage>(), degreeOfParallelism, plan, tracker, metadataStep);
		TimestampCorrectionPipelineStage timestampPool = new(_loggerFactory.CreateLogger<TimestampCorrectionPipelineStage>(), degreeOfParallelism, plan, tracker, timestampStep);
		HashPipelineStage hashPool = new(_loggerFactory.CreateLogger<HashPipelineStage>(), degreeOfParallelism, hashStepContext, tracker, hashStep);
		TransferPipelineStage transferPool = new(_loggerFactory.CreateLogger<TransferPipelineStage>(), degreeOfParallelism, plan, tracker, transferStep);
		SidecarGenerationPipelineStage sidecarPool = new(_loggerFactory.CreateLogger<SidecarGenerationPipelineStage>(), degreeOfParallelism, plan, tracker, sidecarStep);


		// Start Pipeline Tasks
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
		Task metadataTask = Task.Run(() => metadataPool.RunAsync(bufferingChannel.Reader, metadataChannel.Writer, ct), ct);
		Task timestampTask = Task.Run(() => timestampPool.RunAsync(metadataChannel.Reader, timestampChannel.Writer, ct), ct);
		Task hashTask = Task.Run(() => hashPool.RunAsync(timestampChannel.Reader, hashChannel.Writer, ct), ct);
		Task transferTask = Task.Run(() => transferPool.RunAsync(hashChannel.Reader, transferChannel.Writer, ct), ct);
		Task sidecarTask = Task.Run(() => sidecarPool.RunAsync(transferChannel.Reader, persistenceChannel.Writer, ct), ct);

		Task completionTask = Task.Run(async () =>
		{
			tracker.SetPhase(BackupPhase.Transferring);

			try
			{
				await foreach(IBackupItem item in persistenceChannel.Reader.ReadAllAsync(ct))
				{
					ct.ThrowIfCancellationRequested();

					long size = item.Metadata.Get<long>(MetadataKey.Length);
					tracker.CompleteItem(
						item.Id,
						item.ResultState,
						size
					);
				}
			} catch(OperationCanceledException) { }

		}, ct);

		await Task.WhenAll(
			producerTask,
			bufferingTask,
			metadataTask,
			timestampTask,
			hashTask,
			transferTask,
			sidecarTask,
			completionTask
		).ConfigureAwait(false);

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

		return result;
	}
}
