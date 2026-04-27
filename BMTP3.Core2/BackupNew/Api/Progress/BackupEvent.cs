using System;
using BMTP3.Core2.BackupNew.Api.Progress.Enums;

namespace BMTP3.Core2.BackupNew.Api.Progress;

/// <summary>
///     Represents a discrete event that occurred during backup.
///     Used for the "recent activity" display in the UI.
/// </summary>
public class BackupEvent
{
    /// <summary>
    ///     When the event occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }
    /// <summary>
    ///     The type of event.
    /// </summary>
    public BackupEventType Type { get; init; }
    /// <summary>
    ///     The file path related to the event (if any).
    /// </summary>
    public string FilePath { get; init; } = string.Empty;
    /// <summary>
    ///     A message describing the event.
    /// </summary>
    public string Message { get; init; } = string.Empty;
    /// <summary>
    ///     Error details if the event is an error.
    /// </summary>
    public string ErrorDetail { get; init; } = string.Empty;
    /// <summary>
    ///     The reason for skipping a file (if applicable).
    /// </summary>
    public SkipReason SkipReason { get; init; }
}
