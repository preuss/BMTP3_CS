namespace BMTP3.Core2.BackupNew.Api.Progress.Enums;

/// <summary>
///     The reason a file was skipped.
/// </summary>
public enum SkipReason
{
    /// <summary>
    ///     No skip reason.
    /// </summary>
    None,
    /// <summary>
    ///     File was unchanged.
    /// </summary>
    Unchanged,
    /// <summary>
    ///     File was excluded by filter.
    /// </summary>
    ExcludedByFilter,
    /// <summary>
    ///     File was too large.
    /// </summary>
    TooLarge,
    /// <summary>
    ///     File was locked by another process.
    /// </summary>
    LockedByProcess,
    /// <summary>
    ///     Other reason.
    /// </summary>
    Other
}
