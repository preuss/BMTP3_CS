namespace BMTP3.Core3;

/// <summary>
/// Represents the current phase of the backup process.
/// Used for progress reporting to track where we are in the pipeline.
/// </summary>
public enum BackupPhase
{
    Scanning,
    Transferring,
    ExtractingMetadata,
    GeneratingHashes,
    CorrectingTimestamps,
    Complete
}
