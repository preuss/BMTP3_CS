namespace BMTP3.Core2.BackupNew.Api.Progress.Enums;

/// <summary>
///     Why the job stopped. None if Completed.
/// </summary>
public enum StopReason
{
    /// <summary>
    ///     No stop reason (job completed normally).
    /// </summary>
    None,
    /// <summary>
    ///     The job was cancelled by the user.
    /// </summary>
    UserCancelled,
    /// <summary>
    ///     The job stopped due to a fatal error.
    /// </summary>
    FatalError
}
