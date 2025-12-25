using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using BMTP3.Core2.BackupNew.Api.Response;
using BMTP3.Core2.BackupNew.Api.Progress;

namespace BMTP3.Core2.BackupNew.Api;

public interface IBackupJobHandle
{
	Task<BackupJobResult> Completion { get; }
	bool IsCompleted { get; }
	void Cancel();
	IAsyncEnumerable<IBackupProgress> ObserveProgress(CancellationToken ct = default);
}
