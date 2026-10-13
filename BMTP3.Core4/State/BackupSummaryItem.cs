namespace BMTP3.Core4.State;

public sealed record BackupSummaryItem
{
	public required string Id { get; init; }
	public required string SourcePath { get; init; }
	public required string RelativeFilePath { get; init; }
	public required string FileName { get; init; }
	public long Length { get; init; }
	public DateTimeOffset? DateCreated { get; init; }
	public DateTimeOffset? LastModified { get; init; }
	public DateTimeOffset? DateAuthored { get; init; }
	public DateTimeOffset? DateAccessed { get; init; }
	public string? DestinationPath { get; init; }

	/// <summary>
	/// The precise outcome of this item from the last run.
	/// </summary>
	public BackupSummaryItemStatus Status { get; init; }

	/// <summary>
	/// Human-readable reason why this item failed in the last run. Null when not failed.
	/// </summary>
	public string? FailureReason { get; init; }

	public DateTimeOffset? CompletedAt { get; init; }

	/// <summary>
	/// Derived flags for quick readability in the session JSON.
	/// Recomputed from <see cref="Status"/> on every save — editing these values
	/// has no effect, because the next save overwrites them.
	/// </summary>
	public BackupSummaryItemComputed Computed => new()
	{
		// True when Status is Succeeded or Skipped.
		IsCompleted = Status is BackupSummaryItemStatus.Succeeded or BackupSummaryItemStatus.Skipped,
	};
}
