using BMTP3.Core2.NetBackupFlow.Models;

namespace BMTP3.Core2.NetBackupFlow.Models
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
        CollisionIndex
    }
}