namespace BMTP3.Core2.BackupNew2.Models;

/// <summary>
/// Represents the current progress of an ongoing backup job.
/// </summary>
public class BackupProgress
{
	/// <summary>
	/// Total number of files estimated to be processed (if available, e.g., from scanner).
	/// </summary>
	public int TotalFiles { get; set; }

	/// <summary>
	/// Number of files processed so far.
	/// </summary>
	public int ProcessedFiles { get; set; }

	/// <summary>
	/// Percentage of completion.
	/// </summary>
	public double PercentageComplete => TotalFiles > 0 ? (double)ProcessedFiles / TotalFiles * 100 : 0;

	/// <summary>
	/// Name of the file currently being processed.
	/// </summary>
	public string CurrentFileName { get; set; } = string.Empty;

	/// <summary>
	/// Description of the current activity (e.g., "Scanning", "Hashing", "Copying").
	/// </summary>
	public string CurrentActivity { get; set; } = string.Empty;
	public int TotalItemsDiscovered { get; internal set; }
	public int ItemsFailed { get; internal set; }
	public int ItemsProcessed { get; internal set; }
	public long BytesProcessed { get; internal set; }
	public int ItemsSkipped { get; internal set; }

	// If BackupItemStatus is an enum or simple type, it can be included directly.
	// If it's a complex object, consider what minimal info is needed for progress.
	// public BackupItemStatus CurrentItemStatus { get; set; } // Example, needs definition
}
