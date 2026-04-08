using BMTP3.Core2.BackupNew.Api.Progress;
using BMTP3.Core2.BackupNew.Api.Response;

namespace BMTP3.Core2.BackupNew.Api;

public interface IBackupJobHandle
{
	Task<BackupJobResult> Completion { get; }
	bool IsCompleted { get; }
	void Cancel();
	IAsyncEnumerable<IBackupProgress> ObserveProgress(CancellationToken ct = default);
}