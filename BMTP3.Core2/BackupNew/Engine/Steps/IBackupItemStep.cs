using BMTP3.Core2.BackupNew.Api.Enums;
using BMTP3.Core2.BackupNew.Domain.Item;

namespace BMTP3.Core2.BackupNew.Engine.Steps;

/// <summary>
///     Represents a single atomic unit of work (step) within a Pipeline Stage.
/// </summary>
/// <typeparam name="TContext">The context type shared by this step.</typeparam>
/// <typeparam name="TResult">The type of the result produced by this step (e.g. for logging/auditing).</typeparam>
public interface IBackupItemStep<TContext, TResult>
{
	/// <summary>
	///     The name of the step (e.g., "Hashing", "Copying", "Decision").
	/// </summary>
	string Name { get; }

	/// <summary>
	///     The progress phase that this step represents.
	///     Used for reporting active file status.
	/// </summary>
	FilePhase Phase { get; }

	TContext Context { get; }

	/// <summary>
	///     Executes the operation on a single item.
	///     The step MUST modify the item's state or metadata directly (Blackboard pattern).
	///     It ALSO returns a result for immediate logging or flow control within the stage.
	/// </summary>
	/// <param name="item">The backup item to process.</param>
	/// <param name="progress">A reporter for bytes processed within this step.</param>
	/// <param name="ct">Cancellation token.</param>
	Task<TResult> ExecuteAsync(IBackupItem item, IProgress<ulong> progress, CancellationToken ct);
}