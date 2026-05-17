namespace BMTP3.Core4.Api.Models.Enums;

/// <summary>
/// Defines the type of source that will be backed up.
/// </summary>
public enum BackupSourceType
{
	/// <summary>
	/// A media device source (e.g. MTP devices such as phones or cameras).
	/// An MTP/PTP device (e.g., Android Phone, Digital Camera) that is accessed via the Media Transfer Protocol.
	/// </summary>
	MediaDevice,

	/// <summary>
	/// A standard filesystem source (local disk, network share, removable media).
	/// A standard local or network drive (e.g., C:\, \\Server\Share).
	/// </summary>
	FileSystem
}