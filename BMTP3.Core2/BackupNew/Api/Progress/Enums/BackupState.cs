namespace BMTP3.Core2.BackupNew.Api.Progress.Enums;

/// <summary>
///     The current lifecycle state of the job.
/// </summary>
public enum BackupState
{
    /// <summary>
    ///     The job is ready to start.
    /// </summary>
    Ready,
    /// <summary>
    ///     The job is currently running.
    /// </summary>
    Running,
    /// <summary>
    ///     The job completed successfully.
    /// </summary>
    Completed,
    /// <summary>
    ///     The job was stopped (cancelled or failed).
    /// </summary>
    Stopped
}
