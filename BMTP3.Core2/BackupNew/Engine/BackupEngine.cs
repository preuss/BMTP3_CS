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

namespace BMTP3.Core2.BackupNew.Engine;
/// <summary>
/// Skeleton BackupEngine
/// </summary>
public class BackupEngine : IBackupEngine
{
	private readonly IDeviceScanner _deviceScanner;
	private readonly IMediaToBackupItemConverter _converter;
	private readonly IStagingDownloader _stagingDownloader;
	private readonly IMetadataReader _metadataReader; // Changed from IMetadataExtractor
	private readonly IItemHasher _itemHasher; // Added IItemHasher
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
		IMetadataReader metadataReader, // Changed from IMetadataExtractor
		IItemHasher itemHasher, // Added IItemHasher
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
		_metadataReader = metadataReader ?? throw new ArgumentNullException(nameof(metadataReader)); // Changed from _metadataExtractor
		_itemHasher = itemHasher ?? throw new ArgumentNullException(nameof(itemHasher)); // Added _itemHasher
		_pathGenerator = pathGenerator ?? throw new ArgumentNullException(nameof(pathGenerator));
		_collisionResolver = collisionResolver ?? throw new ArgumentNullException(nameof(collisionResolver));
		_fileTransfer = fileTransfer ?? throw new ArgumentNullException(nameof(fileTransfer));
		_repository = repository ?? throw new ArgumentNullException(nameof(repository));
		_sidecarGenerator = sidecarGenerator ?? throw new ArgumentNullException(nameof(sidecarGenerator));
		_options = options ?? throw new ArgumentNullException(nameof(options));
		_loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
	}

	public async Task<BackupJobResult> RunAsync(BackupPlan plan, IProgress<BackupProgress> progress, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(plan);
		progress ??= new Progress<BackupProgress>();

		// Prepare result
		var result = new BackupJobResult
		{
			JobName = plan.Name,
			StartTime = DateTime.UtcNow,
			Status = Domain.Job.JobState.Ready
		};

		if(ct.IsCancellationRequested)
		{
			result.EndTime = DateTime.UtcNow;
			result.Status = Domain.Job.JobState.Cancelled;
			result.GlobalErrors.Add("Cancelled before start");
			return result;
		}

		progress.Report(new BackupProgress { CurrentActivity = "Initializing" });

		// Configure Options
		var opts = _options.Value;
		int degreeOfParallelism = opts.DegreeOfParallelism > 0
			? opts.DegreeOfParallelism
			: Math.Max(1, Environment.ProcessorCount / 2);

		// 1. Define Channels
		var scanChannel = Channel.CreateBounded<MediaFileInfo>(new BoundedChannelOptions(opts.ScanChannelCapacity) { SingleWriter = true, SingleReader = false });
		var convertChannel = Channel.CreateBounded<IBackupItem>(new BoundedChannelOptions(opts.ConvertChannelCapacity) { SingleWriter = false, SingleReader = true });
		var bufferingChannel = Channel.CreateBounded<IBackupItem>(new BoundedChannelOptions(opts.StagingChannelCapacity) { SingleWriter = false, SingleReader = true });
		var metadataChannel = Channel.CreateBounded<IBackupItem>(new BoundedChannelOptions(opts.ProcessingChannelCapacity) { SingleWriter = false, SingleReader = true });
		var timestampChannel = Channel.CreateBounded<IBackupItem>(new BoundedChannelOptions(opts.ProcessingChannelCapacity) { SingleWriter = false, SingleReader = true });
		var hashChannel = Channel.CreateBounded<IBackupItem>(new BoundedChannelOptions(opts.ProcessingChannelCapacity) { SingleWriter = false, SingleReader = true });
		var transferChannel = Channel.CreateBounded<IBackupItem>(new BoundedChannelOptions(opts.ProcessingChannelCapacity) { SingleWriter = false, SingleReader = true });
		var sidecarChannel = Channel.CreateBounded<IBackupItem>(new BoundedChannelOptions(opts.ProcessingChannelCapacity) { SingleWriter = false, SingleReader = true });
		var persistenceChannel = Channel.CreateBounded<IBackupItem>(new BoundedChannelOptions(opts.ProcessingChannelCapacity) { SingleWriter = false, SingleReader = true }); // Final output

		result.Status = Domain.Job.JobState.Running;
		progress.Report(new BackupProgress { CurrentActivity = "Running" });

		// 2. Instantiate Steps
		var bufferingStep = new ContentBufferingItemStep(_stagingDownloader, progress);
		var metadataStep = new MetadataExtractionItemStep(_metadataReader, plan); // Changed from _metadataExtractor
		var timestampStep = new TimestampCorrectionItemStep(plan);
		var hashStepContext = new HashStepContext()
		{
			HashTypes = new List<HashType> { HashType.BLAKE3_512 },
			ForceRecompute = false
		};
		var hashStep = new HashItemStep(hashStepContext, _itemHasher, _loggerFactory.CreateLogger<HashItemStep>()); // Added _itemHasher and ILogger
		var transferStep = new TransferItemStep(plan, _pathGenerator, _collisionResolver, _fileTransfer);
		var sidecarStep = new SidecarGenerationItemStep(plan, _sidecarGenerator);

		// 3. Instantiate Worker Pools
		// We use GenericItemStepWorkerPool because the steps update the item state internally.
//		var bufferingPool = new GenericItemStepWorkerPool<bool>(_loggerFactory.CreateLogger<GenericItemStepWorkerPool<bool>>());
//		var metadataPool = new GenericItemStepWorkerPool<bool>(_loggerFactory.CreateLogger<GenericItemStepWorkerPool<bool>>());
//		var timestampPool = new GenericItemStepWorkerPool<bool>(_loggerFactory.CreateLogger<GenericItemStepWorkerPool<bool>>());
		var hashPool = new HashStepWorkerPool(_loggerFactory.CreateLogger<HashStepWorkerPool>(), degreeOfParallelism, hashStep.Context, hashStep);
//		var transferPool = new GenericItemStepWorkerPool<OperationResult>(_loggerFactory.CreateLogger<GenericItemStepWorkerPool<OperationResult>>());
//		var sidecarPool = new GenericItemStepWorkerPool<bool>(_loggerFactory.CreateLogger<GenericItemStepWorkerPool<bool>>());


		// 4. Start Pipeline Tasks

		// [A] Producer: Scan device
		var producerTask = Task.Run(async () =>
		{
			try
			{
				await foreach(var mediaInfo in _deviceScanner.ScanAsync(plan.SourceId, plan.SourcePath, plan.Recursive, ct))
				{
					ct.ThrowIfCancellationRequested();
					await scanChannel.Writer.WriteAsync(mediaInfo, ct);
					progress.Report(new BackupProgress { CurrentActivity = "Scanning" });
				}
			} catch(OperationCanceledException) { } finally
			{
				scanChannel.Writer.Complete();
			}
		}, ct);

		// [B] Converter: MediaFileInfo -> IBackupItem
		var converterTask = Task.Run(async () =>
		{
			try
			{
				await foreach(var mediaInfo in scanChannel.Reader.ReadAllAsync(ct))
				{
					ct.ThrowIfCancellationRequested();
					var item = _converter.Convert(mediaInfo);
					await convertChannel.Writer.WriteAsync(item, ct);
					// progress.Report(new BackupProgress { CurrentActivity = "Converting" }); // Optional verbose update
				}
			} catch(OperationCanceledException) { } finally
			{
				convertChannel.Writer.Complete();
			}
		}, ct);

		// [C] Pipeline Worker Pools
		// Buffering: Convert -> Buffering
		//		var bufferingTask = Task.Run(() => bufferingPool.RunAsync(plan, bufferingStep, convertChannel.Reader, bufferingChannel.Writer, degreeOfParallelism, ct), ct);

		// Metadata: Buffering -> Metadata
		//		var metadataTask = Task.Run(() => metadataPool.RunAsync(plan, metadataStep, bufferingChannel.Reader, metadataChannel.Writer, degreeOfParallelism, ct), ct);

		// Timestamp: Metadata -> Timestamp
		//var timestampTask = Task.Run(() => timestampPool.RunAsync(plan, timestampStep, metadataChannel.Reader, timestampChannel.Writer, degreeOfParallelism, ct), ct);

		// Hashing: Timestamp -> Hash
		var hashTask = Task.Run(() => hashPool.RunAsync(timestampChannel.Reader, hashChannel.Writer, ct), ct);

		// Transfer: Hash -> Transfer
		//var transferTask = Task.Run(() => transferPool.RunAsync(plan, transferStep, hashChannel.Reader, transferChannel.Writer, degreeOfParallelism, ct), ct);

		// Sidecar: Transfer -> Sidecar
		//var sidecarTask = Task.Run(() => sidecarPool.RunAsync(plan, sidecarStep, transferChannel.Reader, sidecarChannel.Writer, degreeOfParallelism, ct), ct);

		// [D] Final Consumer: Aggregate results from Persistence Channel
		var completionTask = Task.Run(async () =>
		{
			var processedCount = 0;
			var failedCount = 0;
			try
			{
				await foreach(var item in persistenceChannel.Reader.ReadAllAsync(ct))
				{
					ct.ThrowIfCancellationRequested();
					processedCount++;

					if(item.ResultState == ItemResultState.Failed)
					{
						failedCount++;
					}

					// Update aggregate progress
					progress.Report(new BackupProgress
					{
						CurrentActivity = "Processing",
						ProcessedFiles = processedCount,
						ItemsFailed = failedCount
					});
				}
			} catch(OperationCanceledException) { }

			// Store final stats in result
			result.TotalFilesScanned = processedCount; // Should track scanned separately if possible
			result.FilesFailed = failedCount;
			result.FilesCopied = processedCount - failedCount; // Rough estimate
		}, ct);

		// 5. Await Completion

		// Await pipeline completion
		await Task.WhenAll(
			producerTask,
			converterTask,
			//bufferingTask,
			//metadataTask,
			//timestampTask,
			hashTask,
			//transferTask,
			//sidecarTask,
			completionTask
		).ConfigureAwait(false);

		// Finalize result
		result.EndTime = DateTime.UtcNow;
		result.Status = ct.IsCancellationRequested ? Domain.Job.JobState.Cancelled : Domain.Job.JobState.Completed;
		progress.Report(new BackupProgress { CurrentActivity = ct.IsCancellationRequested ? "Cancelled" : "Finished" });

		return result;
	}
}