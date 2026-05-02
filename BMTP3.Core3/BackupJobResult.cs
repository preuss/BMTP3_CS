namespace BMTP3.Core3;

/// <summary>
/// Result of a backup job execution.
/// Summary of what was backed up, how long it took, and any errors.
/// </summary>
public class BackupJobResult
{
	/// <summary>
	/// True if backup completed successfully (all items processed, no critical errors).
	/// False if a critical error occurred (e.g., transfer failure).
	/// </summary>
	public bool Success { get; init; }

	/// <summary>
	/// Total number of items processed.
	/// </summary>
	public int TotalItems { get; init; }

	/// <summary>
	/// Number of items successfully backed up.
	/// </summary>
	public int SuccessfulItems { get; init; }

	/// <summary>
	/// Number of items that failed during backup.
	/// </summary>
	public int FailedItems { get; init; }

	/// <summary>
	/// Total bytes transferred.
	/// </summary>
	public long TotalBytes { get; init; }

	/// <summary>
	/// Time taken for the entire backup operation.
	/// </summary>
	public TimeSpan Duration { get; init; }

	/// <summary>
	/// List of errors that occurred during backup.
   /// May contain non-critical errors even if Success is true.
	/// </summary>
	public List<BackupError> Errors { get; init; } = new();

	/// <summary>
	/// All items that were processed.
	/// Includes both successful and failed items.
	/// </summary>
	public List<BackupItem> Items { get; init; } = new();
}
