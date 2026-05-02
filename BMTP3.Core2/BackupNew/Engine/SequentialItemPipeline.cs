using BMTP3.Core2.BackupNew.Api.Enums;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Internal;

namespace BMTP3.Core2.BackupNew.Engine;

/// <summary>
///     Represents a single step in the backup pipeline.
/// </summary>
public record PipelineStep(
	string Name,
	FilePhase Phase,
	Func<IBackupItem, IProgress<ulong>, CancellationToken, Task> Execute
);

/// <summary>
///     Processes a single item sequentially through a series of pipeline steps.
///     This class eliminates boilerplate by encapsulating the pattern:
///     - Update tracking (for UI progress)
///     - Execute step
///     - Complete item after all steps
/// </summary>
public class SequentialItemPipeline
{
	private readonly ProgressTracker _tracker;
	private readonly List<PipelineStep> _steps;
	private readonly IProgress<ulong> _itemProgress;

	public SequentialItemPipeline(
		ProgressTracker tracker,
		IEnumerable<PipelineStep> steps,
		IProgress<ulong> itemProgress)
	{
		_tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
		_steps = steps?.ToList() ?? throw new ArgumentNullException(nameof(steps));
		_itemProgress = itemProgress ?? throw new ArgumentNullException(nameof(itemProgress));
	}

	/// <summary>
	///     Processes a single item through the entire pipeline sequentially.
	///     For each step:
	///     - Updates tracking (so UI shows current phase)
	///     - Executes the step (which handles errors internally)
	///     After all steps complete, marks item as done with final ResultState.
	/// </summary>
	public async Task ProcessItemAsync(
		IBackupItem item,
		string sourcePath,
		string fileName,
		string relativePath,
		ulong length,
		CancellationToken ct)
	{
		foreach (PipelineStep step in _steps)
		{
			ct.ThrowIfCancellationRequested();

			// Update tracking BEFORE executing step
			// This tells UI which phase/file is currently being processed
			_tracker.UpdateItemPhase(item.Id, sourcePath, fileName, relativePath, step.Phase, length);

			// Execute step (steps handle their own errors via item.Fail())
			await step.Execute(item, _itemProgress, ct).ConfigureAwait(false);

			// If step failed, item.ResultState is already set to Failed
			// Continue to next step (which might also fail/skip)
		}

		// All steps complete: mark item as done globally
		_tracker.CompleteItem(item.Id, item.ResultState, (long)length);
	}
}
