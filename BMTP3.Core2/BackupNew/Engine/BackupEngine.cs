using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BMTP3.Core2.BackupNew.Api;
using BMTP3.Core2.BackupNew.Domain.Job;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Strategies;
using BMTP3.Core2.BackupNew.Infrastructure.Repositories;
using BMTP3.Core2.BackupNew.Api.Enums;
using BMTP3.Core2.BackupNew.Api.Response;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Engine.Traversal;
using BMTP3.Core2.BackupNew.Engine.Staging;
using BMTP3.Core2.BackupNew.Engine.Hashing;
using BMTP3.Core2.BackupNew.Engine.Transfers;
using System.Threading.Channels;
using MediaDevices;
using BMTP3.Core2.BackupNew.Engine.Steps.HashStep;
using BMTP3.Core2.BackupNew.Engine.Orchestration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BMTP3.Core2.BackupNew.Engine.Steps.StagingStep;
using BMTP3.Core2.BackupNew.Engine.Steps.MetadataExtractionStep;
using BMTP3.Core2.BackupNew.Engine.Steps.TimestampCorrectionStep;
using BMTP3.Core2.BackupNew.Engine.Steps.TransferStep;
using BMTP3.Core2.BackupNew.Engine.Steps.SidecarGenerationStep;
using BMTP3.Core2.BackupNew.Engine.Steps;
using BMTP3.Core2.BackupNew.Api.Progress;
using BMTP3.Core2.BackupNew.Engine.Internal;

namespace BMTP3.Core2.BackupNew.Engine;
/// <summary>
/// Skeleton BackupEngine
/// </summary>
public class BackupEngine : IBackupEngine
{
	private readonly IDeviceScanner _deviceScanner;
	private readonly IMediaToBackupItemConverter _converter;
	private readonly IStagingDownloader _stagingDownloader;
	private readonly IMetadataReader _metadataReader;
	private readonly IItemHasher _itemHasher;
	private readonly IPathGenerator _pathGenerator;
	private readonly ICollisionResolver _collisionResolver;
	private readonly IFileTransfer _fileTransfer;
	private readonly IBackupRepository _repository;
	private readonly ISidecarGenerator _sidecarGenerator;
	private readonly IOptions<BackupEngineOptions> _options;
	private readonly ILoggerFactory _loggerFactory;

	public BackupEngine(
		IDeviceScanner deviceScanner,
		IMediaToBackupItemConverter converter,
		IStagingDownloader stagingDownloader,
		IMetadataReader metadataReader,
		IItemHasher itemHasher,
		IPathGenerator pathGenerator,
		ICollisionResolver collisionResolver,
		IFileTransfer fileTransfer,
		IBackupRepository repository,
		ISidecarGenerator sidecarGenerator,
		IOptions<BackupEngineOptions> options,
		ILoggerFactory loggerFactory
	)
	{
		_deviceScanner = deviceScanner ?? throw new ArgumentNullException(nameof(deviceScanner));
		_converter = converter ?? throw new ArgumentNullException(nameof(converter));
		_stagingDownloader = stagingDownloader ?? throw new ArgumentNullException(nameof(stagingDownloader));
		_metadataReader = metadataReader ?? throw new ArgumentNullException(nameof(metadataReader));
		_itemHasher = itemHasher ?? throw new ArgumentNullException(nameof(itemHasher));
		_pathGenerator = pathGenerator ?? throw new ArgumentNullException(nameof(pathGenerator));
		_collisionResolver = collisionResolver ?? throw new ArgumentNullException(nameof(collisionResolver));
		_fileTransfer = fileTransfer ?? throw new ArgumentNullException(nameof(fileTransfer));
		_repository = repository ?? throw new ArgumentNullException(nameof(repository));
		_sidecarGenerator = sidecarGenerator ?? throw new ArgumentNullException(nameof(sidecarGenerator));
		_options = options ?? throw new ArgumentNullException(nameof(options));
		_loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
	}

	public async Task<BackupJobResult> RunAsync(BackupPlan plan, IProgress<IBackupProgress> progress, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(plan);
		progress ??= new Progress<IBackupProgress>();

		// Prepare result
		var tracker = new ProgressTracker();
		tracker.SetPhase(BackupPhase.Starting);

		var result = new BackupJobResult
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

		var reportingTask = Task.Run(async () =>
		{
			while(!ct.IsCancellationRequested)
			{
				progress.Report(tracker.GetSnapshot());
				await Task.Delay(250, ct);
			}
		}, ct);

		// Configure Options
		var opts = _options.Value;
		int degreeOfParallelism = opts.DegreeOfParallelism > 0
			? opts.DegreeOfParallelism
			: Math.Max(1, Environment.ProcessorCount / 2);

		// Define Channels
		var scanChannel = Channel.CreateBounded<MediaFileInfo>(new BoundedChannelOptions(opts.ScanChannelCapacity) { SingleWriter = true, SingleReader = false });
		var convertChannel = Channel.CreateBounded<IBackupItem>(new BoundedChannelOptions(opts.ConvertChannelCapacity) { SingleWriter = false, SingleReader = true });
		var bufferingChannel = Channel.CreateBounded<IBackupItem>(new BoundedChannelOptions(opts.StagingChannelCapacity) { SingleWriter = false, SingleReader = true });
		var metadataChannel = Channel.CreateBounded<IBackupItem>(new BoundedChannelOptions(opts.ProcessingChannelCapacity) { SingleWriter = false, SingleReader = true });
		var timestampChannel = Channel.CreateBounded<IBackupItem>(new BoundedChannelOptions(opts.ProcessingChannelCapacity) { SingleWriter = false, SingleReader = true });
		var hashChannel = Channel.CreateBounded<IBackupItem>(new BoundedChannelOptions(opts.ProcessingChannelCapacity) { SingleWriter = false, SingleReader = true });
		var transferChannel = Channel.CreateBounded<IBackupItem>(new BoundedChannelOptions(opts.ProcessingChannelCapacity) { SingleWriter = false, SingleReader = true });
		var sidecarChannel = Channel.CreateBounded<IBackupItem>(new BoundedChannelOptions(opts.ProcessingChannelCapacity) { SingleWriter = false, SingleReader = true });
		var persistenceChannel = Channel.CreateBounded<IBackupItem>(new BoundedChannelOptions(opts.ProcessingChannelCapacity) { SingleWriter = false, SingleReader = true });

		result.Status = JobState.Running;
		tracker.SetPhase(BackupPhase.Traversing);

		// Instantiate Steps
		var bufferingStep = new ContentBufferingItemStep(_stagingDownloader, progress);
		var metadataStep = new MetadataExtractionItemStep(_metadataReader, plan);
		var timestampStep = new TimestampCorrectionItemStep(plan);
		var hashStepContext = new HashStepContext()
		{
			HashTypes = new List<HashType> { HashType.BLAKE3_512 },
			ForceRecompute = false
		};
		var hashStep = new HashItemStep(hashStepContext, _itemHasher, _loggerFactory.CreateLogger<HashItemStep>());
		var transferStep = new TransferItemStep(plan, _pathGenerator, _collisionResolver, _fileTransfer);
		var sidecarStep = new SidecarGenerationItemStep(plan, _sidecarGenerator);

		// Source Reading MUST be serial (1 thread) to prevent MTP timeouts and IO thrashing.
		// We ignore degreeOfParallelism for this specific step.
		var bufferingPool = new ContentBufferingPipelineStage(_loggerFactory.CreateLogger<ContentBufferingPipelineStage>(), 1, plan, bufferingStep, tracker);
		
		var metadataPool = new MetadataExtractionPipelineStage(_loggerFactory.CreateLogger<MetadataExtractionPipelineStage>(), degreeOfParallelism, plan, metadataStep, tracker);
		var timestampPool = new TimestampCorrectionPipelineStage(_loggerFactory.CreateLogger<TimestampCorrectionPipelineStage>(), degreeOfParallelism, plan, timestampStep, tracker);
		var hashPool = new HashPipelineStage(_loggerFactory.CreateLogger<HashPipelineStage>(), degreeOfParallelism, hashStep.Context, hashStep, tracker);
		var transferPool = new TransferPipelineStage(_loggerFactory.CreateLogger<TransferPipelineStage>(), degreeOfParallelism, plan, transferStep, tracker);
		var sidecarPool = new SidecarGenerationPipelineStage(_loggerFactory.CreateLogger<SidecarGenerationPipelineStage>(), degreeOfParallelism, plan, sidecarStep, tracker);


		// Start Pipeline Tasks
		var producerTask = Task.Run(async () =>
		{
			try
			{
				await foreach(var mediaInfo in _deviceScanner.ScanAsync(plan.SourceId, plan.SourcePath, plan.Recursive, ct))
				{
					ct.ThrowIfCancellationRequested();
                    tracker.AddDiscovery(false, (long)mediaInfo.Length); 
					await scanChannel.Writer.WriteAsync(mediaInfo, ct);
				}
			} catch(OperationCanceledException) { } finally
			{
				scanChannel.Writer.Complete();
			}
		}, ct);

		var converterTask = Task.Run(async () =>
		{
			try
			{
				await foreach(var mediaInfo in scanChannel.Reader.ReadAllAsync(ct))
				{
					ct.ThrowIfCancellationRequested();
					var item = _converter.Convert(mediaInfo);
					await convertChannel.Writer.WriteAsync(item, ct);
				}
			} catch(OperationCanceledException) { } finally
			{
				convertChannel.Writer.Complete();
			}
		}, ct);

		var bufferingTask = Task.Run(() => bufferingPool.RunAsync(convertChannel.Reader, bufferingChannel.Writer, ct), ct);
		var metadataTask = Task.Run(() => metadataPool.RunAsync(bufferingChannel.Reader, metadataChannel.Writer, ct), ct);
		var timestampTask = Task.Run(() => timestampPool.RunAsync(metadataChannel.Reader, timestampChannel.Writer, ct), ct);
		var hashTask = Task.Run(() => hashPool.RunAsync(timestampChannel.Reader, hashChannel.Writer, ct), ct);
		var transferTask = Task.Run(() => transferPool.RunAsync(hashChannel.Reader, transferChannel.Writer, ct), ct);
		var sidecarTask = Task.Run(() => sidecarPool.RunAsync(transferChannel.Reader, persistenceChannel.Writer, ct), ct); 

		var completionTask = Task.Run(async () =>
		{
            tracker.SetPhase(BackupPhase.Transferring); 

			try
			{
				await foreach(var item in persistenceChannel.Reader.ReadAllAsync(ct))
				{
					ct.ThrowIfCancellationRequested();
                    
                    long size = item.Metadata.Get<long>(MetadataKey.Length);
                    tracker.CompleteItem(
                        item.Metadata.Get<string>(MetadataKey.SourceFullPath) ?? "unknown",
                        item.ResultState,
                        size
                    );
				}
			} catch(OperationCanceledException) { }

		}, ct);

		await Task.WhenAll(
			producerTask,
			converterTask,
			bufferingTask,
			metadataTask,
			timestampTask,
			hashTask,
			transferTask,
			sidecarTask,
			completionTask
		).ConfigureAwait(false);

		result.EndTime = DateTime.UtcNow;
		result.Status = ct.IsCancellationRequested ? JobState.Cancelled : JobState.Completed; 
        
        tracker.SetPhase(ct.IsCancellationRequested ? BackupPhase.Cancelled : BackupPhase.Completed);
        progress.Report(tracker.GetSnapshot());

        var snap = tracker.GetSnapshot();
        result.FilesCopied = snap.FilesSucceeded;
        result.FilesFailed = snap.FilesFailed;
        result.FilesSkipped = snap.FilesSkipped;
        result.TotalFilesScanned = snap.FilesDiscovered;
        result.TotalBytesCopied = snap.BytesProcessed;

		return result;
	}
}
