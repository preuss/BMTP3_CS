using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Internal;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Engine.Steps;

/// <summary>
/// Abstract base class for worker-pools that run a sequence of IBackupItemStep for incoming IBackupItem instances.
/// The base implements the reading/forwarding loop and error handling; subclasses may
/// override OnStepResultAsync to react to individual step results.
/// </summary>
public abstract class AbstractPipelineStage<TContext, TResult> : IPipelineStage<TContext, TResult>
{
	private readonly ILogger _logger;
	private readonly int _parallelism;
	private readonly TContext _context;
	private readonly List<IBackupItemStep<TContext, TResult>> _steps;
	private readonly ProgressTracker _tracker;

	public int Parallelism => _parallelism;
	public TContext Context => _context;
	public IReadOnlyList<IBackupItemStep<TContext, TResult>> Steps => _steps;

	protected AbstractPipelineStage(
		ILogger logger,
		int parallelism,
		TContext context,
		IEnumerable<IBackupItemStep<TContext, TResult>> steps,
		ProgressTracker tracker)
	{
		ArgumentNullException.ThrowIfNull(logger);
		ArgumentNullException.ThrowIfNull(parallelism);
		if(parallelism < 0) throw new ArgumentOutOfRangeException(nameof(parallelism));
		if(parallelism == 0) parallelism = Math.Max(Environment.ProcessorCount / 2, 1);
		ArgumentNullException.ThrowIfNull(context);
		ArgumentNullException.ThrowIfNull(steps);
		ArgumentNullException.ThrowIfNull(tracker);

		_parallelism = parallelism;
		_logger = logger;
		_context = context;
		_steps = steps.ToList();
		_tracker = tracker;

		if(_steps.Count == 0)
		{
			_logger.LogWarning("AbstractPipelineStage created with 0 steps. It will be a pass-through.");
		}
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
			workers.Add(Task.Run(async () => await WorkerLoop(reader, writer, ct), ct));
		}

		// Wait for all workers to finish before completing output channel.
		await Task.WhenAll(workers).ConfigureAwait(false);

		_logger.LogDebug("All workers for Stage completed.");
		writer.Complete();
	}

	private async Task WorkerLoop(ChannelReader<IBackupItem> reader, ChannelWriter<IBackupItem> writer, CancellationToken ct)
	{
		await foreach(IBackupItem? item in reader.ReadAllAsync(ct).ConfigureAwait(false))
		{
			// Not null, because TryRead succeeded.
			IBackupItem forward = item;

			// Execute all steps in sequence for the item.
			foreach(var step in Steps)
			{
				if(ct.IsCancellationRequested)
				{
					_logger.LogWarning("Cancellation requested. Worker loop exiting before executing step {StepName} for item {SourceFileName}.", step.Name, forward.Metadata.Get<string>(MetadataKey.SourceFileName));
					ct.ThrowIfCancellationRequested();
				}
				if(forward.ResultState == ItemResultState.Failed)
				{
					// Previous step failed it
					_logger.LogWarning("Item {SourceFileName} is marked as Failed. Skipping remaining steps in pipeline stage.", forward.Metadata.Get<string>(MetadataKey.SourceFileName));
					break;
				}
				TResult? stepResult = await ExecuteStepAsync(forward, step, ct).ConfigureAwait(false);

				if(forward.ResultState == ItemResultState.Failed)
				{
					// Current step failed, stop processing further steps for this item.
					break;
				}

				if(stepResult is null)
				{
					throw new InvalidOperationException($"Step {step.Name} returned null result for item {forward.Metadata.Get<string>(MetadataKey.SourceFileName)}.");
				}
			}
			// Forward to next stage ONCE, after all steps are done (or if failed).
			if(!ct.IsCancellationRequested)
			{
				try
				{
					await writer.WriteAsync(forward, ct).ConfigureAwait(false);
				} catch(OperationCanceledException)
				{
					// Graceful shutdown
				} catch(Exception ex)
				{
					_logger.LogError(ex, "Failed to write item {SourceFileName} to next channel. Worker terminating.", forward.Metadata.Get<string>(MetadataKey.SourceFileName));
					return;
				}
			}
		}
	}

	private async Task<TResult?> ExecuteStepAsync(
		IBackupItem forward,
		IBackupItemStep<TContext, TResult> step,
		CancellationToken ct
	)
	{
		TResult? result = default;

		try
		{
			// Update progress tracker (Thread-safety depends on ProgressTracker implementation)
			string sourcePath = forward.Metadata.Get<string>(MetadataKey.SourceFullPath) ?? "unknown";
			string fileName = forward.Metadata.Get<string>(MetadataKey.SourceFileName) ?? "unknown";
			string relativePath = forward.Metadata.Get<string>(MetadataKey.SourceRelativePath) ?? fileName;
			long size = forward.Metadata.Get<long>(MetadataKey.Length);

			_tracker.UpdateItemPhase(sourcePath, fileName, relativePath, step.Phase, size);

			_logger.LogDebug("Executing step {StepName} for item {SourceFileName}", step.Name, fileName);

			// --- EXECUTE STEP ---
			// We get the result back, even though the Item is also mutated.
			// Execute step and allow subclass to handle the result.
			result = await step.ExecuteAsync(forward, ct).ConfigureAwait(false);

			// Allow subclass/hook to react to the result (e.g. specialized logging)
			await OnStepResultAsync(forward, step, result, ct).ConfigureAwait(false);

			_logger.LogDebug("ItemStep {StepName} completed for item {SourceFileName}", step.Name, fileName);
		} catch(OperationCanceledException) when(ct.IsCancellationRequested)
		{
			_logger.LogWarning("ItemStep {StepName} for item {SourceFileName} was cancelled.", step.Name, forward.Metadata.Get<string>(MetadataKey.SourceFileName));
			throw;
		} catch(Exception ex)
		{
			_logger.LogError(ex, "ItemStep '{StepName}' failed for item '{SourceFileName}': {ErrorMessage}", step.Name, forward.Metadata.Get<string>(MetadataKey.SourceFileName), ex.Message);
			// Mark the item as failed but continue processing other items.
			try
			{
				forward.Fail($"ItemStep '{step.Name}' failed: {ex.Message}", step.Name, ex);
			} catch
			{
				/* swallow fail-safety */
				_logger.LogError(ex, "Failed to mark item as failed after step error.");
			}
		}
		return result;
	}

	/// <summary>
	/// Hook for subclasses to process the TResult returned by a specific step.
	/// Default implementation is a no-op.
	/// </summary>
	protected virtual Task OnStepResultAsync(IBackupItem item, IBackupItemStep<TContext, TResult> step, TResult result, CancellationToken ct)
	{
		return Task.CompletedTask;
	}
}
