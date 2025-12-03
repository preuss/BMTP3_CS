namespace BMTP3.Core2.BackupNew.Models;

/// <summary>
/// Defines the specific action to be taken for a backup item.
/// </summary>
public enum BackupActionType
{
	/// <summary>
	/// Action has not yet been determined.
	/// </summary>
	Unknown,

	/// <summary>
	/// The file should be copied/written to the destination.
	/// </summary>
	Copy,

	/// <summary>
	/// The file should be skipped (e.g., identical file exists, or filtered out).
	/// </summary>
	Skip,

	/// <summary>
	/// The file should be renamed during copy/move to the destination.
	/// </summary>
	Rename,
}
