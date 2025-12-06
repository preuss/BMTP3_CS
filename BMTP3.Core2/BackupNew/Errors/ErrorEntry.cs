namespace BMTP3.Core2.BackupNew.Models;

/// <summary>
/// Represents a single error that occurred during a backup operation.
/// </summary>
public class ErrorEntry
{
	public required string StageName { get; init; }

	/// <summary>
	/// The step in the pipeline where the error occurred (e.g., "Analysis", "Storage").
	/// </summary>
	public required string StepName { get; init; }

	/// <summary>
	/// The primary error message.
	/// </summary>
	public required string Message { get; init; }

	/// <summary>
	/// Timestamp when the error was recorded.
	/// </summary>
	public DateTime Timestamp { get; init; } = DateTime.UtcNow;

	/// <summary>
	/// The exception that caused the error, if applicable.
	/// </summary>
	public Exception? Exception { get; init; }

	/// <summary>
	/// The type name of the exception, if an exception occurred.
	/// </summary>
	public string? ExceptionType { get; init; }

	/// <summary>
	/// The stack trace of the exception, if an exception occurred.
	/// </summary>
	public string? StackTrace { get; init; }

	public override string ToString()
	{
		return $"[{Timestamp:HH:mm:ss}] {StageName}: Error in {StepName}: {Message} {(Exception != null ? $"({Exception.Message})" : "")}";
	}
}