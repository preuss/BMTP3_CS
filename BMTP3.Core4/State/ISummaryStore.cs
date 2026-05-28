namespace BMTP3.Core4.State;

internal interface ISummaryStore
{
	Task SaveAsync(BackupSummary summary);
	Task<BackupSummary?> LoadAsync();
	Task DeleteAsync();
}