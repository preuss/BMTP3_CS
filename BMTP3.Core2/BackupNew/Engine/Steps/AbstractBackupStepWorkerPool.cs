using BMTP3.Core2.BackupNew.Domain.Item; // For MetadataKey, IBackupItem
using BMTP3.Core2.BackupNew.Engine.Internal;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Engine.Steps;

/// <summary>
/// Abstract base class for worker-pools that run a given IBackupItemStep for incoming IBackupItem instances.
/// The base implements the reading/forwarding loop and error handling; subclasses may
/// override OnResultAsync to react to the step result.
/// </summary>
public abstract class AbstractBackupStepWorkerPool<TContext, TResult> : IBackupStepWorkerPool<TContext, TResult>
{
	private readonly ILogger _logger;
	private readonly int _parallelism;
	private readonly TContext _context;
	private readonly IBackupItemStep<TContext, TResult> _step;
    private readonly ProgressTracker _tracker;

	public int Parallelism => _parallelism;
	public TContext Context => _context;
	public IBackupItemStep<TContext, TResult> ItemStep => _step;

	protected AbstractBackupStepWorkerPool(
        ILogger logger, 
        int parallelism, 
        TContext context, 
        IBackupItemStep<TContext, TResult> step,
        ProgressTracker tracker)
	{
		ArgumentNullException.ThrowIfNull(logger);
		ArgumentNullException.ThrowIfNull(parallelism);
		if(parallelism < 0) throw new ArgumentOutOfRangeException(nameof(parallelism));
		if(parallelism == 0) parallelism = Math.Max(Environment.ProcessorCount / 2, 1);
		ArgumentNullException.ThrowIfNull(context);
		ArgumentNullException.ThrowIfNull(step);
        ArgumentNullException.ThrowIfNull(tracker);

		_parallelism = parallelism;
		_logger = logger;
		_context = context;
		_step = step;
        _tracker = tracker;
	}

	public async Task RunAsync(
		ChannelReader<IBackupItem> reader,
		ChannelWriter<IBackupItem> writer,
		CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(reader);
		ArgumentNullException.ThrowIfNull(writer);

		List<Task> workers = new List<Task>(Parallelism);
		for(int i = 0; i < Parallelism; i++)
		{
			workers.Add(Task.Run(async () =>
			{
				await foreach(IBackupItem? item in reader.ReadAllAsync(ct).ConfigureAwait(false))
				{
					IBackupItem forward = item;
					try
					{
                        string sourcePath = forward.Metadata.Get<string>(MetadataKey.SourceFullPath) ?? "unknown";
                        string fileName = forward.Metadata.Get<string>(MetadataKey.SourceFileName) ?? "unknown";
                        string relativePath = forward.Metadata.Get<string>(MetadataKey.SourceRelativePath) ?? fileName;
                        long size = forward.Metadata.Get<long>(MetadataKey.Length);

                        _tracker.UpdateItemPhase(sourcePath, fileName, relativePath, ItemStep.Phase, size);

						_logger.LogDebug("Executing step {StepName} for item {SourceFileName}", ItemStep.Name, fileName);

						TResult? result = await ItemStep.ExecuteAsync(forward, ct).ConfigureAwait(false);
						await OnResultAsync(forward, result, ct).ConfigureAwait(false);

						_logger.LogDebug("ItemStep {StepName} completed for item {SourceFileName}", ItemStep.Name, fileName);
					} catch(OperationCanceledException) when(ct.IsCancellationRequested)
					{
						_logger.LogWarning("ItemStep {StepName} for item {SourceFileName} was cancelled.", ItemStep.Name, forward.Metadata.Get<string>(MetadataKey.SourceFileName));
						throw;
					} catch(Exception ex)
					{
						_logger.LogError(ex, "ItemStep '{StepName}' failed for item '{SourceFileName}': {ErrorMessage}", ItemStep.Name, forward.Metadata.Get<string>(MetadataKey.SourceFileName), ex.Message);
						try { forward.Fail($"ItemStep '{ItemStep.Name}' failed: {ex.Message}", ItemStep.Name, ex); } catch
						{
							_logger.LogError(ex, "Failed to mark item as failed after step error.");
						}
					}

					try
					{
						await writer.WriteAsync(forward, ct).ConfigureAwait(false);
					} catch(OperationCanceledException) when(ct.IsCancellationRequested)
					{
						_logger.LogWarning("Forwarding cancelled for item {SourceFileName} from step {StepName}.", forward.Metadata.Get<string>(MetadataKey.SourceFileName), ItemStep.Name);
						throw;
					} catch(Exception ex)
					{
						_logger.LogError(ex, "Failed to write item {SourceFileName} to next channel from step {StepName}. Terminating worker.", forward.Metadata.Get<string>(MetadataKey.SourceFileName), ItemStep.Name);
						return;
					}
				}
			}, ct));
		}
		await Task.WhenAll(workers).ConfigureAwait(false);
		_logger.LogDebug("All workers for step {StepName} completed.", ItemStep.Name);
		writer.Complete();
	}

	protected virtual Task OnResultAsync(IBackupItem item, TResult result, CancellationToken ct)
	{
		return Task.CompletedTask;
	}
}
