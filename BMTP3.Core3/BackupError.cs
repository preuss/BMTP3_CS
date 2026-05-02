namespace BMTP3.Core3;

/// <summary>
/// Represents a single error that occurred during backup.
/// </summary>
public class BackupError
{
	public required string ItemName { get; init; }
	public required string Message { get; init; }
	public Exception? Exception { get; init; }
}
