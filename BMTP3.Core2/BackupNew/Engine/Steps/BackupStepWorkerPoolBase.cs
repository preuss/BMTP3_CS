using BMTP3.Core2.BackupNew.Domain.Item;
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
	public async Task RunAsync(
		TContext context,
		IBackupItemStep<TContext, TResult> step,
		ChannelReader<IBackupItem> reader,
		ChannelWriter<IBackupItem> writer,
		int parallelism,
		CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(step);
		ArgumentNullException.ThrowIfNull(reader);
		ArgumentNullException.ThrowIfNull(writer);
		if(parallelism < 0) throw new ArgumentOutOfRangeException(nameof(parallelism));
		if(parallelism == 0) parallelism = Math.Max(Environment.ProcessorCount / 2, 1);

		var workers = new List<Task>(parallelism);
		for(int i = 0; i < parallelism; i++)
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
						// Execute step and allow subclass to handle the result.
						TResult? result = await step.ExecuteAsync(context, forward, ct).ConfigureAwait(false);
						await OnResultAsync(context, forward, result, ct).ConfigureAwait(false);
					} catch(OperationCanceledException) when(ct.IsCancellationRequested)
					{
						throw;
					} catch(Exception ex)
					{
						// Mark the item as failed but continue processing other items.
						try { forward.Fail($"Step '{step.Name}' failed: {ex.Message}", step.Name, ex); } catch { /* swallow */ }
					}

					// Forward the (possibly updated) item to the next stage.
					try
					{
						await writer.WriteAsync(forward, ct).ConfigureAwait(false);
					} catch(OperationCanceledException) when(ct.IsCancellationRequested)
					{
						throw;
					} catch
					{
						// Writer failed (likely completed). Terminate this worker.
						return;
					}
				}
			}, ct));
		}
		// Wait for all workers to finish before completing output channel.
		await Task.WhenAll(workers).ConfigureAwait(false);
		writer.Complete();
	}

	/// <summary>
	/// Hook for subclasses to process the TResult returned by the step.
	/// Default implementation is a no-op.
	/// </summary>
	protected virtual Task OnResultAsync(TContext context, IBackupItem item, TResult result, CancellationToken ct)
	{
		return Task.CompletedTask;
	}
}