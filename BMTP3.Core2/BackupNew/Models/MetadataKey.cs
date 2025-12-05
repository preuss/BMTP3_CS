using BMTP3.Core2.BackupNew2.Models;

namespace BMTP3.Core2.BackupNew.Models;
/// <summary>
/// All known metadata keys.
/// Using enum + attribute provides both type safety and the ability to serialize to readable strings.
/// </summary>
public enum MetadataKey
	{
		// File identification and integrity
		[KeyStringValue("source_id")]
		SourceId,// MTP PersistentUniqueId or FileInfo.FullName
		[KeyStringValue("source_full_path")]
		SourceFullPath,
		[KeyStringValue("source_relative_path")]
		SourceRelativePath,// E.g. "DCIM\100APPLE\" on the phone
		[KeyStringValue("source_file_name")]
		SourceFileName,
		// length - Also stored in ISourceContent.Length
		[KeyStringValue("length")]
		Length,

		// ── Results from stages (enrichment) ─────────────────
		[KeyStringValue("hashes")] // Can contain a Dictionary<string, string> e.g. {"SHA256": "...", "MD5": "..."}
		Hashes,

		// Device-specific information (from MTP/PTP)
		[KeyStringValue("device_name")]
		DeviceName,
		[KeyStringValue("device_file_url")]
		DeviceFileUrl,
		[KeyStringValue("device_unique_id")]
		DeviceUniqueId,

		// Normalized timestamps
		[KeyStringValue("datetime_authored")]
		AuthoredDateTime,         // Primary date (EXIF 'Date Taken', MTP 'Authored Date')
		[KeyStringValue("datetime_modified")]
		ModifiedDateTime,         // File content last changed (LastWriteTime / mtime)
		[KeyStringValue("datetime_created")]
		CreatedDateTime,          // File created on filesystem (CreationTime / btime)
		[KeyStringValue("datetime_accessed")]
		LastAccessDateTime,       // File last read (LastAccessTime / atime)
		[KeyStringValue("datetime_metadata_changed")]
		MetadataChangeDateTime,   // Metadata last changed (ctime)

		// Target handling
		[KeyStringValue("collision_index")]
		CollisionIndex,
		[KeyStringValue("local_temp_path")]
		LocalTempPath,// string – full path to temporary file
		[KeyStringValue("final_target_path")]
		FinalTargetPath,// string – where the file ends up in the backup folder
	}
