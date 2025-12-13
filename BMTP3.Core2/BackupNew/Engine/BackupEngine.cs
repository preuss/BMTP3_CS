using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BMTP3.Core2.BackupNew.Api;
using BMTP3.Core2.BackupNew.Domain.Job;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Strategies;
using BMTP3.Core2.BackupNew.Engine.Steps;
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

namespace BMTP3.Core2.BackupNew.Engine;
/// <summary>
/// Skeleton BackupEngine
/// </summary>
public class BackupEngine : IBackupEngine
{
	private readonly IDeviceScanner _deviceScanner;
	private readonly IMediaToBackupItemConverter _converter;
	private readonly IStagingDownloader _stagingDownloader;
	private readonly IMetadataExtractor _metadataExtractor;
	private readonly IHashGenerator _hashGenerator;
	private readonly IPathGenerator _pathGenerator;
	private readonly ICollisionResolver _collisionResolver;
	private readonly IFileTransfer _fileTransfer;
	private readonly IBackupRepository _repository;

	public BackupEngine(
		IDeviceScanner deviceScanner,
		IMediaToBackupItemConverter converter,
		IStagingDownloader stagingDownloader,
		IMetadataExtractor metadataExtractor,
		IHashGenerator hashGenerator,
		IPathGenerator pathGenerator,
		ICollisionResolver collisionResolver,
		IFileTransfer fileTransfer,
		IBackupRepository repository)
	{
		_deviceScanner = deviceScanner ?? throw new ArgumentNullException(nameof(deviceScanner));
		_converter = converter ?? throw new ArgumentNullException(nameof(converter));
		_stagingDownloader = stagingDownloader ?? throw new ArgumentNullException(nameof(stagingDownloader));
		_metadataExtractor = metadataExtractor ?? throw new ArgumentNullException(nameof(metadataExtractor));
		_hashGenerator = hashGenerator ?? throw new ArgumentNullException(nameof(hashGenerator));
		_pathGenerator = pathGenerator ?? throw new ArgumentNullException(nameof(pathGenerator));
		_collisionResolver = collisionResolver ?? throw new ArgumentNullException(nameof(collisionResolver));
		_fileTransfer = fileTransfer ?? throw new ArgumentNullException(nameof(fileTransfer));
		_repository = repository ?? throw new ArgumentNullException(nameof(repository));
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

		// Channels (use IBackupItem for processing pipeline so worker pools can interoperate)
		var scanChannel = Channel.CreateBounded<MediaFileInfo>(new BoundedChannelOptions(128) { SingleWriter = true, SingleReader = false });
		var convertChannel = Channel.CreateBounded<IBackupItem>(new BoundedChannelOptions(128) { SingleWriter = false, SingleReader = true });
		var stagingToProcessingChannel = Channel.CreateBounded<IBackupItem>(new BoundedChannelOptions(128) { SingleWriter = false, SingleReader = true });

		// Chain of three hash stages for demonstration
		var postHash1 = Channel.CreateBounded<IBackupItem>(new BoundedChannelOptions(128) { SingleWriter = false, SingleReader = true });
		var postHash2 = Channel.CreateBounded<IBackupItem>(new BoundedChannelOptions(128) { SingleWriter = false, SingleReader = true });
		var postHash3 = Channel.CreateBounded<IBackupItem>(new BoundedChannelOptions(128) { SingleWriter = false, SingleReader = true });

		result.Status = Domain.Job.JobState.Running;
		progress.Report(new BackupProgress { CurrentActivity = "Running" });

		// Producer: scan device (single-threaded device access)
		var producer = Task.Run(async () =>
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

		// Converter: MediaFileInfo -> IBackupItem
		var converterTask = Task.Run(async () =>
		{
			try
			{
				await foreach(var mediaInfo in scanChannel.Reader.ReadAllAsync(ct))
				{
					ct.ThrowIfCancellationRequested();
					var item = _converter.Convert(mediaInfo);
					await convertChannel.Writer.WriteAsync(item, ct);
					progress.Report(new BackupProgress { CurrentActivity = "Converting" });
				}
			} catch(OperationCanceledException) { } finally
			{
				convertChannel.Writer.Complete();
			}
		}, ct);

		// Staging: download to local temp file (single-threaded)
		var stagingRoot = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "bmtp3_staging", Guid.NewGuid().ToString("n"));
		var stagingTask = Task.Run(async () =>
		{
			try
			{
				await foreach(IBackupItem item in convertChannel.Reader.ReadAllAsync(ct))
				{
					ct.ThrowIfCancellationRequested();
					await _stagingDownloader.DownloadToStagingAsync(item, stagingRoot, progress, ct);
					await stagingToProcessingChannel.Writer.WriteAsync(item, ct);
					progress.Report(new BackupProgress { CurrentActivity = "Staged" });
				}
			} catch(OperationCanceledException) { } finally
			{
				stagingToProcessingChannel.Writer.Complete();
			}
		}, ct);

		// Prepare hash step implementation (noop adapter for demo)
		HashItemStep hashStep = new();

		// Start three HashStepWorkerPool instances chained: stagingToProcessing -> postHash1 -> postHash2 -> postHash3
		HashStepWorkerPool hashPool1 = new();
		HashStepWorkerPool hashPool2 = new();
		HashStepWorkerPool hashPool3 = new();

		int degreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2);

		Task hashPoolTask1 = Task.Run(() => hashPool1.RunAsync(plan, hashStep, stagingToProcessingChannel.Reader, postHash1.Writer, degreeOfParallelism, ct), ct);
		Task hashPoolTask2 = Task.Run(() => hashPool2.RunAsync(plan, hashStep, postHash1.Reader, postHash2.Writer, degreeOfParallelism, ct), ct);
		Task hashPoolTask3 = Task.Run(() => hashPool3.RunAsync(plan, hashStep, postHash2.Reader, postHash3.Writer, degreeOfParallelism, ct), ct);

		// Final processing: read items after the third hash stage and update progress/counters.
		Task processingTask = Task.Run(async () =>
		{
			var processed = 0;
			try
			{
				await foreach(var staged in postHash3.Reader.ReadAllAsync(ct))
				{
					ct.ThrowIfCancellationRequested();
					processed++;
					progress.Report(new BackupProgress { CurrentActivity = "ProcessedAfterHashChain", ProcessedFiles = processed });
				}
			} catch(OperationCanceledException) { }
		}, ct);

		// Await pipeline completion
		await Task.WhenAll(producer, converterTask, stagingTask, hashPoolTask1, hashPoolTask2, hashPoolTask3, processingTask).ConfigureAwait(false);

		// Finalize result
		result.EndTime = DateTime.UtcNow;
		result.Status = ct.IsCancellationRequested ? Domain.Job.JobState.Cancelled : Domain.Job.JobState.Completed;
		progress.Report(new BackupProgress { CurrentActivity = ct.IsCancellationRequested ? "Cancelled" : "Finished" });

		return result;
	}
}