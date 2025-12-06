using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Job.StateMachine;
/// <summary>
/// Validates transitions for backup job lifecycle.
/// </summary>
public sealed class JobStateMachine : IStateMachine<JobState>
{
	private static readonly Dictionary<JobState, JobState[]> JobTransitions =
		new()
		{
				{ JobState.Ready,     new[] { JobState.Running } },
				{ JobState.Running,   new[] { JobState.Completed, JobState.Failed, JobState.Cancelled } },
				{ JobState.Completed, Array.Empty<JobState>() },
				{ JobState.Failed,    Array.Empty<JobState>() },
				{ JobState.Cancelled, new[] { JobState.Running } } // resume supported
		};

	public bool CanTransition(JobState from, JobState to) =>
		JobTransitions.TryGetValue(from, out var allowed) && Array.IndexOf(allowed, to) >= 0;

	/// <summary>
	/// Applies a job state transition and writes an audit entry.
	/// </summary>
	public void Apply(BackupJob job, JobState to)
	{
		if(!CanTransition(job.State, to))
			throw new InvalidOperationException($"Invalid job transition: {job.State} -> {to}");

		job.State = to;

		job.AuditTrail.Add(new AuditItemEntry
		{
			Stage = $"Job:{to}",
			AttemptCount = (uint)(job.AuditTrail.Count + 1),
			LifecycleState = default,   // not relevant for jobs
			ResultState = default,      // not relevant for jobs
			Timestamp = DateTime.UtcNow,
			ErrorSummary = null
		});
	}
}