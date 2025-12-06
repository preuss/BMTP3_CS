using BMTP3.Core2.BackupNew.Domain.Job;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Engine.StateMachine;
/// <summary>
/// Validates transitions for backup job lifecycle.
/// </summary>
public sealed class JobStateMachine : IStateMachine<JobState>
{
	private static readonly Dictionary<JobState, HashSet<JobState>> JobTransitions =
		new()
		{
			{ JobState.Ready,     new HashSet<JobState> { JobState.Running } },
			{ JobState.Running,   new HashSet<JobState> { JobState.Completed, JobState.Failed, JobState.Cancelled } },
			{ JobState.Completed, new HashSet<JobState>() },
			{ JobState.Failed,    new HashSet<JobState>() },
			{ JobState.Cancelled, new HashSet<JobState> { JobState.Running } }
		};

	public bool CanTransition(JobState from, JobState to) =>
		JobTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

	/// <summary>
	/// Applies a job state transition and writes an audit entry.
	/// </summary>
	public void Apply(BackupJob job, JobState to)
	{
		if(!CanTransition(job.State, to))
		{
			throw new InvalidOperationException($"Invalid job transition: {job.State} -> {to}");
		}

		job.TransitionTo(to, $"Job transitioned to {to}");
	}
}