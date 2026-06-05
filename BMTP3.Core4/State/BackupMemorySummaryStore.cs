namespace BMTP3.Core4.State;

internal sealed class BackupMemorySummaryStore : ISummaryStore
{
	private BackupSummary? _summary;

	public Task SaveAsync(BackupSummary summary, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		_summary = summary;
		return Task.CompletedTask;
	}

	public Task<BackupSummary?> LoadAsync()
	{
		return Task.FromResult(_summary);
	}

	public Task DeleteAsync()
	{
		_summary = null;
		return Task.CompletedTask;
	}
}
