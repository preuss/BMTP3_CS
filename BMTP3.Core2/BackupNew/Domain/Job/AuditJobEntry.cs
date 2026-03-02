namespace BMTP3.Core2.BackupNew.Domain.Job;
/// <summary>
/// Snapshot of a backup job state transition.
/// </summary>
public class AuditJobEntry
{
	/// <summary>
	/// JobState of the job when this entry was recorded (e.g., Ready, Running...).
	/// </summary>
	public required JobState State { get; init; }

	/// <summary>
	/// Timestamp of when this entry was recorded.
	/// </summary>
	public required DateTime Timestamp { get; init; }

	/// <summary>
	/// Optional summary (e.g., "Job started", "Job completed with errors").
	/// </summary>
	public string? Summary { get; init; }

	/// <summary>
	/// Number of items in the job at this point.
	/// </summary>
	public int ItemCount { get; set; }

	/// <summary>
	/// Number of times the job has been attempted/resumed.
	/// </summary>
	public uint AttemptCount { get; init; }
}