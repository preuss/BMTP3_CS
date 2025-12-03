namespace BMTP3.Core2.BackupNew2.Models.Configuration
{
	public enum MetadataKey
	{
		// Fil-identifikation og integritet
		[KeyStringValue("source_id")]
		SourceId,
		[KeyStringValue("source_full_path")]
		SourceFullPath,
		[KeyStringValue("hashes")] // Kan indeholde en Dictionary<string, string> f.eks. {"SHA256": "...", "MD5": "..."}
		Hashes,

		// Enheds-specifik information (fra MTP/PTP)
		[KeyStringValue("device_name")]
		DeviceName,
		[KeyStringValue("device_file_url")]
		DeviceFileUrl,
		[KeyStringValue("device_unique_id")]
		DeviceUniqueId,

		// Normaliserede tidsstempler
		[KeyStringValue("datetime_authored")]
		AuthoredDateTime,         // Primær dato (EXIF 'Date Taken', MTP 'Authored Date')
		[KeyStringValue("datetime_modified")]
		ModifiedDateTime,         // Filens indhold sidst ændret (LastWriteTime / mtime)
		[KeyStringValue("datetime_created")]
		CreatedDateTime,          // Filen oprettet på filsystemet (CreationTime / btime)
		[KeyStringValue("datetime_accessed")]
		LastAccessDateTime,       // Filen sidst læst (LastAccessTime / atime)
		[KeyStringValue("datetime_metadata_changed")]
		MetadataChangeDateTime,   // Metadata sidst ændret (ctime)

		// Kollisionshåndtering
		[KeyStringValue("collision_index")]
		CollisionIndex,
		[KeyStringValue("length")]
		Length,
		[KeyStringValue("local_temp_path")]
		LocalTempPath,
		[KeyStringValue("original_file_name")]
		OriginalFileName,
		[KeyStringValue("source_relative_path")]
		SourceRelativePath,
		[KeyStringValue("original_source_id")]
		OriginalSourceId,
		[KeyStringValue("hash_sha_256")]
		HashSha256,
		[KeyStringValue("final_target_path")]
		FinalTargetPath,
	}
}