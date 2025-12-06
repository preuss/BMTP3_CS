using BMTP3.Core2.BackupNew.Domain.Item;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Engine.StateMachine;
/// <summary>
/// Validates transitions for item lifecycle and result states.
/// Writes audit entries directly to the item via internal helpers.
/// </summary>
public sealed class ItemStateMachine
{
	private static readonly Dictionary<LifecycleState, LifecycleState[]> LifecycleTransitions =
		new()
		{
				{ LifecycleState.New,       new[] { LifecycleState.Queued } },
				{ LifecycleState.Queued,    new[] { LifecycleState.Active } },
				{ LifecycleState.Active,    new[] { LifecycleState.Processed } },
				{ LifecycleState.Processed, Array.Empty<LifecycleState>() }
		};

	private static readonly Dictionary<ResultState, ResultState[]> ResultTransitions =
		new()
		{
				{ ResultState.Pending, new[] { ResultState.Success, ResultState.Failed, ResultState.Skipped } },
				{ ResultState.Success, Array.Empty<ResultState>() },
				{ ResultState.Failed,  Array.Empty<ResultState>() },
				{ ResultState.Skipped, Array.Empty<ResultState>() }
		};

	private static bool Contains<T>(T[] array, T value) => Array.IndexOf(array, value) >= 0;

	/// <summary>
	/// Returns true if the lifecycle transition is allowed.
	/// </summary>
	public bool CanTransitionLifecycle(LifecycleState from, LifecycleState to) =>
		LifecycleTransitions.TryGetValue(from, out var allowed) && Contains(allowed, to);

	/// <summary>
	/// Returns true if the result transition is allowed.
	/// </summary>
	public bool CanTransitionResult(ResultState from, ResultState to) =>
		ResultTransitions.TryGetValue(from, out var allowed) && Contains(allowed, to);

	/// <summary>
	/// Queues an item for processing.
	/// </summary>
	public void Queue(BackupItem item)
	{
		if(!CanTransitionLifecycle(item.LifecycleState, LifecycleState.Queued))
			throw new InvalidOperationException($"Invalid lifecycle transition: {item.LifecycleState} -> Queued");

		item.SetQueued();
	}

	/// <summary>
	/// Marks an item as active (processing started).
	/// </summary>
	public void Activate(BackupItem item)
	{
		if(!CanTransitionLifecycle(item.LifecycleState, LifecycleState.Active))
			throw new InvalidOperationException($"Invalid lifecycle transition: {item.LifecycleState} -> Active");

		item.SetActive();
	}

	/// <summary>
	/// Applies a result transition to the item and updates lifecycle if needed.
	/// </summary>
	public void ApplyResult(BackupItem item, ResultState to, string? errorSummary = null)
	{
		if(!CanTransitionResult(item.ResultState, to))
			throw new InvalidOperationException($"Invalid result transition: {item.ResultState} -> {to}");

		item.SetResult(to, errorSummary);

		if(to == ResultState.Failed && !string.IsNullOrWhiteSpace(errorSummary))
		{
			item.Errors.AddError("Result", errorSummary, DateTime.UtcNow);
		}
	}
}
