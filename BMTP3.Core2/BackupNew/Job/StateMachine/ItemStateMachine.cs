using BMTP3.Core2.BackupNew.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Job.StateMachine;
/// <summary>
/// Validates transitions for item lifecycle and result states.
/// </summary>
public sealed class ItemStateMachine
{
	private static readonly Dictionary<LifecycleState, LifecycleState[]> LifecycleTransitions =
		new()
		{
			{ LifecycleState.New, new[] { LifecycleState.Queued } },
			{ LifecycleState.Queued, new[] { LifecycleState.Active } },
			{ LifecycleState.Active, new[] { LifecycleState.Processed } },
			{ LifecycleState.Processed, Array.Empty<LifecycleState>() }
		};

	private static readonly Dictionary<ResultState, ResultState[]> ResultTransitions =
		new()
		{
			{ ResultState.Pending, new[] { ResultState.Success, ResultState.Failed, ResultState.Skipped } },
			{ ResultState.Success, Array.Empty<ResultState>() },
			{ ResultState.Failed, Array.Empty<ResultState>() },
			{ ResultState.Skipped, Array.Empty<ResultState>() }
		};

	/// <summary>
	/// Returns true if the lifecycle transition is allowed.
	/// </summary>
	public bool CanTransitionLifecycle(LifecycleState from, LifecycleState to) =>
		LifecycleTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

	/// <summary>
	/// Returns true if the result transition is allowed.
	/// </summary>
	public bool CanTransitionResult(ResultState from, ResultState to) =>
		ResultTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

	/// <summary>
	/// Applies a result transition to the item and updates lifecycle if needed.
	/// </summary>
	public void ApplyResult(BackupItem item, ResultState to, Action<AuditEntry> auditLog)
	{
		if(!CanTransitionResult(item.ResultState, to))
			throw new InvalidOperationException($"Invalid result transition: {item.ResultState} -> {to}");

		item.ResultState = to;

		// Move lifecycle to Processed after any terminal result.
		if(item.LifecycleState == LifecycleState.Active &&
			CanTransitionLifecycle(item.LifecycleState, LifecycleState.Processed))
		{
			item.LifecycleState = LifecycleState.Processed;
		}

		auditLog(new AuditEntry
		{
			Stage = "Result",
			Error = to == ResultState.Failed ? item.Metadata?.LastError : null,
			AttemptCount = item.Metadata?.AttemptCount ?? 0,
			Timestamp = DateTime.UtcNow
		});
	}

	/// <summary>
	/// Queues an item for processing.
	/// </summary>
	public void Queue(BackupItem item, Action<AuditEntry> auditLog)
	{
		if(!CanTransitionLifecycle(item.LifecycleState, LifecycleState.Queued))
			throw new InvalidOperationException($"Invalid lifecycle transition: {item.LifecycleState} -> Queued");

		item.LifecycleState = LifecycleState.Queued;
		item.ResultState = ResultState.Pending;

		auditLog(new AuditEntry
		{
			Stage = "Queue",
			Error = null,
			AttemptCount = item.Metadata?.AttemptCount ?? 0,
			Timestamp = DateTime.UtcNow
		});
	}

	/// <summary>
	/// Marks an item as active (processing started).
	/// </summary>
	public void Activate(BackupItem item, Action<AuditEntry> auditLog)
	{
		if(!CanTransitionLifecycle(item.LifecycleState, LifecycleState.Active))
			throw new InvalidOperationException($"Invalid lifecycle transition: {item.LifecycleState} -> Active");

		item.LifecycleState = LifecycleState.Active;

		auditLog(new AuditEntry
		{
			Stage = "Activate",
			Error = null,
			AttemptCount = item.Metadata?.AttemptCount ?? 0,
			Timestamp = DateTime.UtcNow
		});
	}
}
