namespace BMTP3.Core4.State;

/// <summary>
/// Derived totals for quick readability in the session JSON.
/// Recomputed over <see cref="BackupSummary.Items"/> on every save and never read by the
/// engine — present purely as information for whoever reads the file.
/// </summary>
public sealed record BackupSummaryComputed
{
	public int TotalFiles { get; init; }
	public long TotalBytes { get; init; }
	public int CompletedFiles { get; init; }
	public long CompletedBytes { get; init; }
	public bool IsComplete { get; init; }
}

/// <summary>
/// Derived flags for a single <see cref="BackupSummaryItem"/>.
/// Recomputed from <see cref="BackupSummaryItem.Status"/> on every save —
/// editing these values has no effect, they are overwritten on the next save.
/// </summary>
public sealed record BackupSummaryItemComputed
{
	public bool IsCompleted { get; init; }
}
