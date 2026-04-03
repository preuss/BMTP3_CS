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

	/// <summary>
	/// Duration of the stage in milliseconds, if available.
	/// </summary>
	public long? DurationMs { get; set; }

	/// <summary>
	/// Optional worker identifier that processed the item for correlation.
	/// </summary>
	public string? WorkerId { get; set; }

	/// <summary>
	/// Optional number of items processed in a batch for this entry.
	/// </summary>
	public int? ItemCount { get; set; }
}
