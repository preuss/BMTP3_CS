namespace BMTP3.Core2.BackupNew2.Models.Configuration;

// --------------------------------------------------
// SOURCE TYPES
// --------------------------------------------------

/// <summary>
/// Defines the type of source location.
/// </summary>
public enum SourceType
{
    FileSystem,     // A standard local or network drive (e.g., C:\, \\Server\Share)
    MediaDevice     // An MTP/PTP device (e.g., Android Phone, Digital Camera)
}

// --------------------------------------------------
// STRATEGIES (Enums)
// --------------------------------------------------

/// <summary>Defines the strategy for creating the destination folder structure.</summary>
public enum OutputStructureStrategy
{
    PreserveSourceTree, // Preserves the original folder hierarchy from the source.
    Flat,               // Places all files directly into the root destination folder.
    CustomPathPattern,  // Uses the PathPattern to define the entire path, including folders and filename.
}

/// <summary>Defines the type of resolution method to be applied when a file collision is detected.</summary>
public enum CollisionResolutionType
{
    Overwrite, // Overwrites the existing file.
    Skip,      // Skips the current file.
    Error,     // Stops the backup process.
    Rename     // Renames the new file using a specific strategy.
}

/// <summary>Defines how the existing file in the destination is compared to the source file before CollisionResolutionTypes is applied.</summary>
public enum CollisionComparisonType
{
    None,    // Skips content comparison. Proceeds directly to CollisionResolutionTypes (Overwrite, Rename, etc.).
    Hash,    // Compares file content using a hash algorithm (e.g., SHA-256). Skips copy if hashes are identical.
    Binary   // Compares file content byte-by-byte. Skips copy if files are bit-for-bit identical.
}

/// <summary>Defines the strategy for renaming a file when CollisionResolutionTypes is 'Rename'.</summary>
public enum RenameStrategy
{
    Increment,                 // Appends a sequential number (e.g., file_1.ext).
    Timestamp,                 // Appends a timestamp to the file name.
    Hash,                      // Appends a short content hash to the file name.
    CustomCollisionPathPattern // Uses a custom pattern defined in CollisionPathPattern.
}

/// <summary>Defines the type of metadata file (sidecar) to be created next to each backed-up file.</summary>
public enum SidecarFormat
{
    None,       // No metadata file is created per file.
    Ini,        // Creates an INI file (e.g., file.jpg.ini) next to each file.
    Json,       // Creates a JSON file (e.g., file.jpg.json) next to each file.
}

/// <summary>Defines the centralized catalog or index structure for the entire backup set.</summary>
public enum BackupIndexType
{
    None,       // No central metadata file or database is created.
    Json,       // Creates a single JSON file containing all backup metadata (e.g., backup_catalog.json).
    Database,   // Creates a centralized SQLite database containing all metadata (e.g., backup.db).
}
