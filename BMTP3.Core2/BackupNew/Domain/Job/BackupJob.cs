using BMTP3.Core2.BackupNew.Domain.Item;

namespace BMTP3.Core2.BackupNew.Domain.Job;

/// <summary>
///     Represents a backup job containing multiple items and audit history.
/// </summary>
public class BackupJob
{
	private readonly List<AuditJobEntry> _auditTrail;

	/// <summary>
	///     Creates a new backup job with an initial state and optional items.
	/// </summary>
	public BackupJob(string? name = null, IEnumerable<BackupItem>? items = null)
	{
		JobId = Guid.NewGuid();
		Name = name ?? JobId.ToString("b");

		State = JobState.Ready;
		Items = items?.ToList() ?? new List<BackupItem>();
		_auditTrail = new List<AuditJobEntry>();

		AddAudit(JobState.Ready, "Job created");
	}

	public Guid JobId { get; }
	public string? Name { get; }

	/// <summary>
	///     Current lifecycle state of the job.
	/// </summary>
	public JobState State { get; private set; }

	/// <summary>
	///     Collection of items included in this job.
	/// </summary>
	public List<BackupItem> Items { get; }

	/// <summary>
	///     Audit trail of job state transitions (job-level only).
	/// </summary>
	public IReadOnlyList<AuditJobEntry> AuditTrail => _auditTrail;

	/// <summary>
	///     Number of times the job has been attempted/resumed.
	/// </summary>
	public uint AttemptCount { get; private set; }

	internal void TransitionTo(JobState newState, string summary)
	{
		if (newState == State)
		{
			throw new Exception("Job is already in the specified state.");
		}

		State = newState;
		if (newState == JobState.Running)
		{
			AttemptCount++;
		}

		AddAudit(newState, summary);
	}

	private void AddAudit(JobState state, string summary)
	{
		_auditTrail.Add(new AuditJobEntry
		{
			State = state,
			Timestamp = DateTime.UtcNow,
			Summary = summary,
			ItemCount = Items.Count,
			AttemptCount = AttemptCount
		});
	}
}