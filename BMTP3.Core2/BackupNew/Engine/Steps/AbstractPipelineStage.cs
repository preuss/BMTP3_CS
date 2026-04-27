using System.Threading.Channels;
using BMTP3.Core2.BackupNew.Api.Progress.Enums;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Internal;
using Microsoft.Extensions.Logging;

namespace BMTP3.Core2.BackupNew.Engine.Steps;

/// <summary>
///     Abstract base class for worker-pools that run a sequence of IBackupItemStep for incoming IBackupItem instances.
///     The base implements the reading/forwarding loop and error handling; subclasses may
///     override OnStepResultAsync to react to individual step results.
///     Abstract base class for worker-pools (Stages).
///     Manages the worker threads, channel consumption, and error reporting.
///     Subclasses must implement ProcessItemAsync to define the logic (Steps) for this stage.
/// </summary>
public abstract class AbstractPipelineStage<TContext> : IPipelineStage<TContext>
{
	private readonly ILogger _logger;
	protected readonly ProgressTracker _tracker;

	protected AbstractPipelineStage(
		ILogger logger,
		int parallelism,
		TContext context,
		ProgressTracker tracker)
	{
		ArgumentNullException.ThrowIfNull(logger);
		if (parallelism < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(parallelism));
		}

		if (parallelism == 0)
		{
			parallelism = Math.Max(Environment.ProcessorCount / 2, 1);
		}

		ArgumentNullException.ThrowIfNull(context);
		ArgumentNullException.ThrowIfNull(tracker);

		Parallelism = parallelism;
		_logger = logger;
		Context = context;
		_tracker = tracker;
	}

	public int Parallelism { get; }

	public TContext Context { get; }

	public async Task RunAsync(
		ChannelReader<IBackupItem> reader,
		ChannelWriter<IBackupItem> writer,
		CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(reader);
		ArgumentNullException.ThrowIfNull(writer);

		List<Task> workers = new(Parallelism);
		for (int i = 0; i < Parallelism; i++)
		{
			workers.Add(Task.Run(async () => await WorkerLoop(reader, writer, ct), ct));
		}

		// Wait for all workers to finish before completing output channel.
		// IMPORTANT: writer.Complete() must be called in a finally block so that downstream
		// stages are never left waiting on a channel that will never receive more items.
		// Without this, an unhandled exception in any worker would cause all downstream
		// pipeline stages to deadlock indefinitely on ReadAllAsync().
		Exception? workerException = null;
		try
		{
			await Task.WhenAll(workers).ConfigureAwait(false);
			_logger.LogDebug("All workers for Stage completed.");
		}
		catch (Exception ex)
		{
			workerException = ex;
			_logger.LogError(ex, "One or more workers for Stage faulted.");
		}
		finally
		{
			// TryComplete propagates the exception as the channel's completion cause,
			// which allows downstream ReadAllAsync to throw rather than hang.
			writer.TryComplete(workerException);
		}
	}

	private async Task WorkerLoop(ChannelReader<IBackupItem> reader, ChannelWriter<IBackupItem> writer,
		CancellationToken ct)
	{
		await foreach (IBackupItem? item in reader.ReadAllAsync(ct).ConfigureAwait(false))
		{
			IBackupItem forward = item;

			if (forward.ResultState == ItemResultState.Failed)
			{
				// Item failed in a previous stage. Forward it directly without processing.
				try
				{
					await writer.WriteAsync(forward, ct).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Failed to forward FAILED item {SourceFileName}. Worker terminating.",
						forward.Metadata.Get<string>(MetadataKey.SourceFileName));
					return;
				}

				continue;
			}

			try
			{
				// --- TEMPLATE METHOD CALL ---
				await ProcessItemAsync(forward, ct).ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				_logger.LogWarning("Processing cancelled for item {SourceFileName}.",
					forward.Metadata.Get<string>(MetadataKey.SourceFileName));
				throw;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Stage failed processing item {SourceFileName}: {Message}",
					forward.Metadata.Get<string>(MetadataKey.SourceFileName), ex.Message);
				try
				{
					forward.Fail($"Stage failed: {ex.Message}", "PipelineStage", ex);
				}
				catch
				{
					/* swallow fail-safety */
				}
			}

			// Forward to next stage
			if (!ct.IsCancellationRequested)
			{
				try
				{
					await writer.WriteAsync(forward, ct).ConfigureAwait(false);
				}
				catch (OperationCanceledException)
				{
					// Graceful shutdown
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Failed to write item {SourceFileName} to next channel. Worker terminating.",
						forward.Metadata.Get<string>(MetadataKey.SourceFileName));
					return;
				}
			}
		}
	}

	/// <summary>
	///     Executes the business logic for this stage.
	///     This is where the concrete class calls its Step(s).
	/// </summary>
	protected abstract Task ProcessItemAsync(IBackupItem item, CancellationToken ct);

	/// <summary>
	///     Helper method to create a progress reporter for a specific item.
	///     Concrete stages should call this to get an IProgress reporter to pass to steps.
	/// </summary>
	protected IProgress<ulong> CreateProgressReporter(IBackupItem item)
	{
		string itemId = item.Id;
		return new Progress<ulong>(bytesProcessed => { _tracker.UpdateItemBytes(itemId, bytesProcessed); });
	}

	/// <summary>
	///     Helper method to update the phase for an item.
	///     Concrete stages should call this before executing a step.
	/// </summary>
	protected void UpdatePhase(IBackupItem item, FilePhase phase)
	{
		string itemId = item.Id;
		string sourcePath = item.Metadata.Get<string>(MetadataKey.SourceFullPath) ?? "unknown";
		string fileName = item.Metadata.Get<string>(MetadataKey.SourceFileName) ?? "unknown";
		string relativePath = item.Metadata.Get<string>(MetadataKey.SourceRelativePath) ?? fileName;
		ulong size = item.Metadata.Get<ulong>(MetadataKey.Length);

		_tracker.UpdateItemPhase(itemId, sourcePath, fileName, relativePath, phase, size);
	}
}