namespace BMTP3.Core2.BackupNew2.Models;

/// <summary>
/// Keys used to identify specific pieces of metadata associated with a backup item.
/// </summary>
public enum MetadataKey
{
    // --- Source Identity ---
    OriginalSourceId,       // e.g., Drive letter "C:" or Device Name "iPhone"
    SourceRelativePath,     // Path relative to the source root
    OriginalFileName,       // e.g., "IMG_1234.JPG"
    
    // --- File Attributes ---
    SizeBytes,              // File size in bytes
    DetectedMimeType,       // e.g., "image/jpeg"
    
    // --- Fingerprinting ---
    HashSha256,             // SHA-256 hash of the file content
    
    // --- Timestamps (The Waterfall Model) ---
    AuthoredDateTime,       // The normalized, "true" creation time
    RawExifDate,            // Debug: Date read from EXIF
    RawFileSystemCreated,   // Debug: File System CreationTime
    RawFileSystemModified,  // Debug: File System LastWriteTime
    RawDeviceDate,          // Debug: Date reported by MTP device
    
    // --- Processing Context ---
    LocalTempPath,          // Path to the temporary local copy (for MTP)
    
    // --- Destination ---
    FinalTargetPath,        // The full absolute path where the file is/will be stored
    DestinationRelativePath // Path relative to the Output Root
}
