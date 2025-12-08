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
	private static readonly Dictionary<ItemLifecycleState, ItemLifecycleState[]> LifecycleTransitions =
		new()
		{
				{ ItemLifecycleState.New,       new[] { ItemLifecycleState.Queued } },
				{ ItemLifecycleState.Queued,    new[] { ItemLifecycleState.Active } },
				{ ItemLifecycleState.Active,    new[] { ItemLifecycleState.Processed } },
				{ ItemLifecycleState.Processed, Array.Empty<ItemLifecycleState>() }
		};

	private static readonly Dictionary<ItemResultState, ItemResultState[]> ResultTransitions =
		new()
		{
				{ ItemResultState.Pending, new[] { ItemResultState.Success, ItemResultState.Failed, ItemResultState.Skipped } },
				{ ItemResultState.Success, Array.Empty<ItemResultState>() },
				{ ItemResultState.Failed,  Array.Empty<ItemResultState>() },
				{ ItemResultState.Skipped, Array.Empty<ItemResultState>() }
		};

	private static bool Contains<T>(T[] array, T value) => Array.IndexOf(array, value) >= 0;

	/// <summary>
	/// Returns true if the lifecycle transition is allowed.
	/// </summary>
	public bool CanTransitionLifecycle(ItemLifecycleState from, ItemLifecycleState to) =>
		LifecycleTransitions.TryGetValue(from, out var allowed) && Contains(allowed, to);

	/// <summary>
	/// Returns true if the result transition is allowed.
	/// </summary>
	public bool CanTransitionResult(ItemResultState from, ItemResultState to) =>
		ResultTransitions.TryGetValue(from, out var allowed) && Contains(allowed, to);

	/// <summary>
	/// Queues an item for processing.
	/// </summary>
	public void Queue(BackupItem item)
	{
		if(!CanTransitionLifecycle(item.LifecycleState, ItemLifecycleState.Queued))
			throw new InvalidOperationException($"Invalid lifecycle transition: {item.LifecycleState} -> Queued");

		item.SetQueued();
	}

	/// <summary>
	/// Marks an item as active (processing started).
	/// </summary>
	public void Activate(BackupItem item)
	{
		if(!CanTransitionLifecycle(item.LifecycleState, ItemLifecycleState.Active))
			throw new InvalidOperationException($"Invalid lifecycle transition: {item.LifecycleState} -> Active");

		item.SetActive();
	}

	/// <summary>
	/// Applies a result transition to the item and updates lifecycle if needed.
	/// </summary>
	public void ApplyResult(BackupItem item, ItemResultState to, string? errorSummary = null)
	{
		if(!CanTransitionResult(item.ResultState, to))
			throw new InvalidOperationException($"Invalid result transition: {item.ResultState} -> {to}");

		item.SetResult(to, errorSummary);

		if(to == ItemResultState.Failed && !string.IsNullOrWhiteSpace(errorSummary))
		{
			item.Errors.AddError("Result", errorSummary, DateTime.UtcNow);
		}
	}
}
