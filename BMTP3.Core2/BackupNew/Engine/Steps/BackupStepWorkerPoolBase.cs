using BMTP3.Core2.BackupNew.Domain.Item;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Engine.Steps;

/// <summary>
/// Base worker-pool that runs a given IBackupItemStep for incoming IBackupItem instances.
/// The base implements the reading/forwarding loop and error handling; subclasses may
/// override OnResultAsync to react to the step result.
/// </summary>
public abstract class BackupStepWorkerPoolBase<TContext, TResult> : IBackupStepWorkerPool<TContext, TResult>
{
	private readonly ILogger _logger;
	private readonly int _parallelism;
	private readonly TContext _context; // Context is now fixed for this pool instance
	private readonly IBackupItemStep<TContext, TResult> _step; // Step is now fixed for this pool instance

	public int Parallelism => _parallelism;
	public TContext Context => _context;
	public IBackupItemStep<TContext, TResult> ItemStep => _step;

	protected BackupStepWorkerPoolBase(ILogger logger, int parallelism, TContext context, IBackupItemStep<TContext, TResult> step)
	{
		ArgumentNullException.ThrowIfNull(logger);
		ArgumentNullException.ThrowIfNull(parallelism);
		if(parallelism < 0) throw new ArgumentOutOfRangeException(nameof(parallelism));
		if(parallelism == 0) parallelism = Math.Max(Environment.ProcessorCount / 2, 1);
		ArgumentNullException.ThrowIfNull(context);
		ArgumentNullException.ThrowIfNull(step);

		_parallelism = parallelism;
		_logger = logger;
		_context = context;
		_step = step;
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
				// Use ReadAllAsync to await incoming items without busy-waiting.
				await foreach(IBackupItem? item in reader.ReadAllAsync(ct).ConfigureAwait(false))
				{
					// Not null, because TryRead succeeded.
					IBackupItem forward = item;
					try
					{
						_logger.LogDebug("Executing step {StepName} for item {SourceFileName}", ItemStep.Name, forward.Metadata.Get<string>(MetadataKey.SourceFileName));

						// Execute step and allow subclass to handle the result.
						TResult? result = await ItemStep.ExecuteAsync(forward, ct).ConfigureAwait(false);
						await OnResultAsync(forward, result, ct).ConfigureAwait(false);

						_logger.LogDebug("ItemStep {StepName} completed for item {SourceFileName}", ItemStep.Name, forward.Metadata.Get<string>(MetadataKey.SourceFileName));
					} catch(OperationCanceledException) when(ct.IsCancellationRequested)
					{
						_logger.LogWarning("ItemStep {StepName} for item {SourceFileName} was cancelled.", ItemStep.Name, forward.Metadata.Get<string>(MetadataKey.SourceFileName));
						throw;
					} catch(Exception ex)
					{
						_logger.LogError(ex, "ItemStep '{StepName}' failed for item '{SourceFileName}': {ErrorMessage}", ItemStep.Name, forward.Metadata.Get<string>(MetadataKey.SourceFileName), ex.Message);
						// Mark the item as failed but continue processing other items.
						try { forward.Fail($"ItemStep '{ItemStep.Name}' failed: {ex.Message}", ItemStep.Name, ex); } catch
						{
							/* swallow */
							_logger.LogError(ex, "Failed to mark item as failed after step error.");
						}
					}

					// Forward the (possibly updated) item to the next stage.
					try
					{
						await writer.WriteAsync(forward, ct).ConfigureAwait(false);
					} catch(OperationCanceledException) when(ct.IsCancellationRequested)
					{
						_logger.LogWarning("Forwarding cancelled for item {SourceFileName} from step {StepName}.", forward.Metadata.Get<string>(MetadataKey.SourceFileName), ItemStep.Name);
						throw;
					} catch(Exception ex)
					{
						// Writer failed (likely completed). Terminate this worker.
						_logger.LogError(ex, "Failed to write item {SourceFileName} to next channel from step {StepName}. Terminating worker.", forward.Metadata.Get<string>(MetadataKey.SourceFileName), ItemStep.Name);
						return;
					}
				}
			}, ct));
		}
		// Wait for all workers to finish before completing output channel.
		await Task.WhenAll(workers).ConfigureAwait(false);
		_logger.LogDebug("All workers for step {StepName} completed.", ItemStep.Name);
		writer.Complete();
	}

	/// <summary>
	/// Hook for subclasses to process the TResult returned by the step.
	/// Default implementation is a no-op.
	/// </summary>
	protected virtual Task OnResultAsync(IBackupItem item, TResult result, CancellationToken ct)
	{
		return Task.CompletedTask;
	}
}