namespace BMTP3.Core2.BackupNew2.Models;

/// <summary>
/// Represents the processing state of a single backup item within the pipeline.
/// </summary>
public enum BackupState
{
    /// <summary>
    /// Item has been discovered but processing has not started.
    /// </summary>
    New,

    /// <summary>
    /// Source content is prepared (e.g., copied to temp if MTP).
    /// </summary>
    Prepared,

    /// <summary>
    /// File analysis (hashing, metadata extraction) is completed.
    /// </summary>
    Analyzed,

    /// <summary>
    /// The action to perform (Copy, Skip, Rename) has been decided.
    /// </summary>
    ActionDecided,

    /// <summary>
    /// The file has been successfully committed to the destination (or skipped intentionally).
    /// </summary>
    Completed,

    /// <summary>
    /// Processing failed at some step.
    /// </summary>
    Failed,
	Finalized
}
