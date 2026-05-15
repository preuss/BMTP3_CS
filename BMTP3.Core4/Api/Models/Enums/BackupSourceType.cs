namespace BMTP3.Core4.Api.Models.Enums;

/// <summary>
/// Defines the type of source that will be backed up.
/// </summary>
public enum BackupSourceType
{
	/// <summary>
	/// A media device source (e.g. MTP devices such as phones or cameras).
	/// </summary>
	MediaDevice,

	/// <summary>
	/// A standard filesystem source (local disk, network share, removable media).
	/// </summary>
	FileSystem
}