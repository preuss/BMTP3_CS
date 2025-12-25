using BMTP3.Core2.BackupNew.Api.Enums;

namespace BMTP3.Core2.BackupNew.Api;

/// <summary>
/// Represents the progress and status of a single file currently being processed.
/// </summary>
public class FileProgress
{
    /// <summary>
    /// The name of the file (e.g., "photo.jpg").
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// The relative path of the file from the source root (e.g., "DCIM/Camera/photo.jpg").
    /// Useful for display in the UI.
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>
    /// The full path or unique identifier of the source file.
    /// </summary>
    public string SourcePath { get; set; } = string.Empty;

    /// <summary>
    /// The current processing phase of the file.
    /// </summary>
    public FilePhase Phase { get; set; }

    /// <summary>
    /// The total size of the file in bytes.
    /// </summary>
    public long BytesTotal { get; set; }

    /// <summary>
    /// The number of bytes processed so far for the current operation.
    /// </summary>
    public long BytesProcessed { get; set; }
}