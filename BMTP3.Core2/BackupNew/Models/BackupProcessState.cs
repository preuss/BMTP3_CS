namespace BMTP3.Core2.BackupNew.Models;
/// <summary>
/// Represents the lifecycle states of a backup item during processing.
/// These states describe the *condition* of the item at each step,
/// not the pipeline stage being executed.
/// </summary>
public enum BackupProcessState
{
	/// <summary>
	/// Item has been created but no processing has occurred yet.
	/// It's pending initial actions.
	/// </summary>
	New = 0,

	/// <summary>
	/// File has been copied locally into the working buffer (temp folder).
	/// At this point the item is no longer dependent on the MTP/PTP source.
	/// </summary>
	WorkingBuffer = 10,

	/// <summary>
	/// Metadata has been extracted and hash has been created.
	/// This state indicates that the item has been fully analyzed.
	/// </summary>
	Analyzed = 20,

	/// <summary>
	/// Temp file has been normalized (timestamps corrected).
	/// For example, Created/Modified times are updated to reflect
	/// the actual authored date rather than NOW() from the device.
	/// </summary>
	Normalized = 30,

	/// <summary>
	/// Metadata is sufficient for sidecar creation.
	/// This means the item has all required information to generate
	/// a valid sidecar file.
	/// </summary>
	SidecarReady = 40,

	/// <summary>
	/// Final destination path has been calculated and assigned.
	/// The item now knows exactly where it will be stored.
	/// </summary>
	DestinationAssigned = 50,

	/// <summary>
	/// File and sidecar have been committed to the destination.
	/// This state indicates that the copy/move operation has succeeded.
	/// </summary>
	Committed = 60
}
