namespace BMTP3.Core4.State;
internal interface ISummaryStore
{
	Task SaveAsync(BackupSummary summary, CancellationToken cancellationToken);
	Task<BackupSummary?> LoadAsync(CancellationToken cancellationToken);
	Task DeleteAsync(CancellationToken cancellationToken);
}