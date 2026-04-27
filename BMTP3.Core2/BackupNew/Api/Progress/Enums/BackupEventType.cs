namespace BMTP3.Core2.BackupNew.Api.Progress.Enums;

/// <summary>
///     The type of event that occurred during backup.
/// </summary>
public enum BackupEventType
{
    /// <summary>
    ///     Scanning started.
    /// </summary>
    ScanStarted,
    /// <summary>
    ///     Error while scanning directory.
    /// </summary>
    ScanDirectoryError,
    /// <summary>
    ///     Scanning completed.
    /// </summary>
    ScanCompleted,
    /// <summary>
    ///     File copy started.
    /// </summary>
    FileCopyStarted,
    /// <summary>
    ///     File copy completed.
    /// </summary>
    FileCopyCompleted,
    /// <summary>
    ///     File was skipped.
    /// </summary>
    FileSkipped,
    /// <summary>
    ///     File copy failed.
    /// </summary>
    FileFailed,
    /// <summary>
    ///     Retrying file copy.
    /// </summary>
    FileRetrying,
    /// <summary>
    ///     Backup phase changed.
    /// </summary>
    PhaseChanged,
    /// <summary>
    ///     Warning event.
    /// </summary>
    Warning
}
