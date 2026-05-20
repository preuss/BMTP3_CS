namespace BMTP3.Core4.State;

public sealed record BackupSummary
{
	public required string SessionId { get; init; }

	public required string SourceRoot { get; init; }

	/// <summary>
	/// When this summary snapshot was created.
	/// </summary>
	public DateTimeOffset CreatedAt { get; init; }

	public List<BackupSummaryItem> Items { get; init; } = new();

	// Computed from Items — no need to store redundant counts.
	public int TotalFiles => Items.Count;

	public long TotalBytes => Items.Sum(i => i.Length);

	public int CompletedFiles => Items.Count(i => i.IsCompleted);

	public long CompletedBytes => Items.Where(i => i.IsCompleted).Sum(i => i.Length);

	public bool IsComplete => Items.Count > 0 && CompletedFiles == TotalFiles;
}
