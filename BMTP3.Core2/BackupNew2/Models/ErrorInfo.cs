using System;

namespace BMTP3.Core2.BackupNew2.Models;

/// <summary>
/// Captures error information related to a specific backup item.
/// </summary>
public class ErrorInfo
{
    /// <summary>
    /// The primary error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The exception that caused the error, if applicable.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// The step in the pipeline where the error occurred (e.g., "Analysis", "Storage").
    /// </summary>
    public string StepName { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the error was recorded.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    public override string ToString()
    {
        return $"[{Timestamp:HH:mm:ss}] Error in {StepName}: {Message} {(Exception != null ? $"({Exception.Message})" : "")}";
    }
}
