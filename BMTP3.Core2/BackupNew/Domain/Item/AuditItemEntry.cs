namespace BMTP3.Core2.BackupNew.Domain.Item;
/// <summary>
/// Represents a single audit entry for a backup job.
/// </summary>
public class AuditItemEntry
{
	/// <summary>
	/// Stage of the job when this entry was recorded (e.g., Copy, Verify).
	/// </summary>
	public required string Stage { get; init; }

	/// <summary>
	/// Number of attempts made at this stage.
	/// </summary>
	public uint AttemptCount { get; init; }

	/// <summary>
	/// The current lifecycle state of the item.
	/// </summary>
	public required ItemLifecycleState LifecycleState { get; init; }

	/// <summary>
	/// The current result state of the item.
	/// </summary>
	public required ItemResultState ResultState { get; init; }

	/// <summary>
	/// Timestamp of when this entry was recorded.
	/// </summary>
	public required DateTime Timestamp { get; init; }

	/// <summary>
	/// Summary of any errors that occurred during this stage.
	/// </summary>
	public string? ErrorSummary { get; set; }

	// TODO: Additional fields like Duration, ItemCount, etc. can be added as needed.
	// TODO: We can expand later example with: WorkerId, Duration, ResultState.
}
