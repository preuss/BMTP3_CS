namespace BMTP3.Core4.State;

internal interface ISummaryStore
{
	/// <summary>Location of the persisted store. Null if not file-backed (e.g. in-memory).</summary>
	FileInfo? StoreFile { get; }
	Task SaveAsync(BackupSummary summary, CancellationToken cancellationToken);
	Task<BackupSummary?> LoadAsync();
	Task DeleteAsync();
}