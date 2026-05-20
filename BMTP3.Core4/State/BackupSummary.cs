namespace BMTP3.Core4.State;

public sealed record BackupSummary
{
	public required string SessionId { get; init; }

	public required string SourceRoot { get; init; }

	public DateTime GeneratedUtc { get; init; }

	public List<BackupSummaryItem> Items { get; init; } = new();

	public int TotalFiles { get; init; }

	public long TotalBytes { get; init; }

	public int CompletedFiles { get; init; }

	public long CompletedBytes { get; init; }

	public bool IsComplete => CompletedFiles == TotalFiles;
}
