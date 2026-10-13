namespace BMTP3.Core4.State;

public sealed record BackupSummary
{
	/// <summary>
	/// Version of the session file format this build writes and requires.
	/// Sessions written by any other version fail to load — the format is not backward compatible.
	/// </summary>
	public const int CurrentVersion = 2;

	/// <summary>
	/// Format version this session was written with. Assigned when saving.
	/// Null for legacy files written before versioning — treated as version 1.
	/// </summary>
	public int? SessionVersion { get; init; }

	public required string SessionId { get; init; }

	public required string SourceRoot { get; init; }

	/// <summary>
	/// When this summary snapshot was created.
	/// </summary>
	public DateTimeOffset CreatedAt { get; init; }

	// Computed from Items — no need to store redundant counts.
	// Grouped in one section so readers can tell derived values from persisted data they may edit.
	// Declared before Items so a session file opens with the totals, not with 25k item blocks.
	public BackupSummaryComputed Computed
	{
		get
		{
			int completedFiles = Items.Count(i => i.Computed.IsCompleted);

			return new BackupSummaryComputed
			{
				TotalFiles = Items.Count,
				TotalBytes = Items.Sum(i => i.Length),
				CompletedFiles = completedFiles,
				CompletedBytes = Items.Where(i => i.Computed.IsCompleted).Sum(i => i.Length),
				IsComplete = Items.Count > 0 && completedFiles == Items.Count,
			};
		}
	}

	public List<BackupSummaryItem> Items { get; init; } = new();
}