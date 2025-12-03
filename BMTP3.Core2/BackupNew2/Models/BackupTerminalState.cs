namespace BMTP3.Core2.BackupNew2.Models;

/// <summary>
/// Represents the terminal states of a backup item.
/// These states describe the *final outcome* of processing.
/// </summary>
public enum BackupTerminalState
{
	/// <summary>
	/// No final outcome yet (still in progress).
	/// </summary>
	Unknown = 0,

	/// <summary>
	/// Processing completed successfully.
	/// The item has reached its intended destination and is finalized.
	/// Or it has been skipped.
	/// </summary>
	Completed = 10,

	/// <summary>
	/// Processing failed irrecoverably.
	/// The item could not be backed up, but other items may continue.
	/// </summary>
	Failed = 20
}