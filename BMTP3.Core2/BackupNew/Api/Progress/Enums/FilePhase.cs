namespace BMTP3.Core2.BackupNew.Api.Enums;

/// <summary>
/// Represents the specific processing stage of a single file within the backup pipeline.
/// </summary>
public enum FilePhase
{
    /// <summary>
    /// The file is being downloaded or copied from the source device to a temporary staging area.
    /// </summary>
    Staging,

    /// <summary>
    /// Metadata (timestamps, EXIF data) is being extracted or corrected.
    /// </summary>
    Metadata,

    /// <summary>
    /// A cryptographic hash (checksum) of the file content is being calculated.
    /// </summary>
    Hashing,

    /// <summary>
    /// The engine is determining the destination path and resolving any file collisions (e.g., deciding to rename or skip).
    /// </summary>
    Planning,

    /// <summary>
    /// The file is being transferred to its final destination. This includes copy/move operations and cleanup.
    /// </summary>
    Transferring
}