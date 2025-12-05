namespace BMTP3.Core2.BackupNew.Models;
/// <summary>
/// Defines all possible states an IBackupItem can be in.
/// The order is important – we use it for validation in BackupItem.AdvanceTo().
/// </summary>
public enum BackupState
{
	/// <summary>
	/// Item has been discovered but processing has not started.
	/// </summary>
	Pending = 0,

	/// <summary>
	/// The file has been downloaded/copied to a local temp folder (if necessary).
	/// Content now points to a local file.
	/// </summary>
	Staged = 10,

	/// <summary>
	/// Source content is prepared (e.g., copied to temp if MTP).
	/// </summary>
	Prepared,

	/// <summary>
	/// File analysis (hashing, metadata extraction) is completed.
	/// </summary>
	Analyzed = 20,

	/// <summary>
	/// The action to perform (Copy, Skip, Rename) has been decided.
	/// </summary>
	ActionDecided,

	/// <summary>
	/// Decision made: Should be copied, skipped due to duplicate, etc.
	/// </summary>
	Decided = 30,

	/// <summary>
	/// The file has been successfully committed to the destination (or skipped intentionally).
	/// </summary>
	Completed,

	/// <summary>
	/// The file has been copied to the final destination and any sidecar file written.
	/// Item is fully processed.
	/// </summary>
	Committed = 40,

	/// <summary>
	/// Processing failed at some step. The process continues with other files.
	/// </summary>
	Failed = 90,

	/// <summary>
	/// The item has reached its final state in the pipeline (e.g., after cleanup or post-processing).
	/// </summary>
	Finalized,

	/// <summary>
	/// Intentionally skipped (e.g. already exists with same hash and date).
	/// </summary>
	Skipped = 95
}