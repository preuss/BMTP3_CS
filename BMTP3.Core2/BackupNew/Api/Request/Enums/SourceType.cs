namespace BMTP3.Core2.BackupNew.Api.Request.Enums;

/// <summary>
/// Defines the type of source location.
/// </summary>
public enum SourceType
{
	FileSystem,     // A standard local or network drive (e.g., C:\, \\Server\Share)
	MediaDevice     // An MTP/PTP device (e.g., Android Phone, Digital Camera)
}
