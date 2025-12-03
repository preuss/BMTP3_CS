namespace BMTP3.Core2.BackupNew.Models;
/// <summary>
/// Represents the terminal states of a backup item.
/// These states describe the *final outcome* of processing.
/// </summary>
public enum BackupTerminalState
{
	/// <summary>
	/// No final outcome yet (still in progress).
	/// </summary>
	None = 0,

	/// <summary>
	/// Processing completed successfully.
	/// The item has reached its intended destination and is finalized.
	/// </summary>
	Completed = 10,

	/// <summary>
	/// Item was intentionally skipped (e.g. duplicate, already present).
	/// This is a valid end state and no further processing occurs.
	/// </summary>
	Skipped = 20,

	/// <summary>
	/// Processing failed irrecoverably.
	/// The item could not be backed up, but other items may continue.
	/// </summary>
	Failed = 30
}