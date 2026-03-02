namespace BMTP3.Core2.BackupNew.Domain.Item;

/// <summary>
/// Defines the origin of a file's timestamp.
/// Used for auditing the waterfall logic.
/// </summary>
public enum TimestampSource
{
	Unknown,
	Exif,           // Internal metadata (Date Taken)
	Xmp,            // Adobe/Standard metadata
	Mtp,            // Device-level metadata (Authored Date)
	FileSystem,     // File creation time (btime)
	LastModified,   // Fallback: Last write time (mtime)
	UserDefined     // Manual override
}
