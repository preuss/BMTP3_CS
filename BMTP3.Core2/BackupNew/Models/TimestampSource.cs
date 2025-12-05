namespace BMTP3.Core2.BackupNew.Models
{
	public enum TimestampSource
	{
		None,
		InternalMetadata,      // EXIF/XMP
		DeviceMetadata,        // MTP/PTP AuthoredDate
		FilesystemCreation,
		FilesystemModification,
		CurrentTime,           // Fallback
		Unknown
	}
}
