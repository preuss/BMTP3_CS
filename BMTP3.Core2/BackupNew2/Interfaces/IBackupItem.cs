using BMTP3.Core2.BackupNew2.Models;

namespace BMTP3.Core2.BackupNew2.Interfaces;

/// <summary>
/// The central context object that flows through the backup pipeline.
/// It holds the content, metadata, and state of a single file being processed.
/// </summary>
public interface IBackupItem
{
    /// <summary>
    /// The physical content source (file system file, MTP object, etc.).
    /// </summary>
    ISourceContent Content { get; }

    /// <summary>
    /// Dynamic metadata container enriched throughout the pipeline.
    /// </summary>
    Metadata Metadata { get; }

    /// <summary>
    /// The current lifecycle state of the item (e.g., New, Analyzed, Completed).
    /// </summary>
    BackupState State { get; set; }

    /// <summary>
    /// The decided action to perform (e.g., Copy, Skip).
    /// </summary>
    BackupActionType Action { get; set; }

    /// <summary>
    /// Information about any error that occurred during processing.
    /// Null if no error has occurred.
    /// </summary>
    ErrorInfo? ErrorInfo { get; set; }

    /// <summary>
    /// Helper method to mark the item as failed.
    /// </summary>
    void Fail(string message, string stepName, System.Exception? ex = null);
}
