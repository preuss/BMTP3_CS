namespace BMTP3.Core4.Models.Enums;

/// <summary>
/// Represents the internal processing status of a backup item.
/// </summary>
internal enum BackupItemStatus
{
	/// <summary>
	/// The item has been discovered but not yet processed.
	/// </summary>
	Pending,

	/// <summary>
	/// The item is currently handled by the backup process.
	///	</summary>
	Active,

	/// <summary>
	/// The item was successfully backed up.
	/// </summary>
	Succeeded,

	/// <summary>
	/// The item was intentionally skipped.
	/// </summary>
	Skipped,

	/// <summary>
	/// The item failed during processing.
	/// </summary>
	Failed
}
