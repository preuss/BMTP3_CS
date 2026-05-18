using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Engine.State;

namespace BMTP3.Core4.Engine.Runner;

internal interface IBackupRunner
{
	Task<BackupResult> RunAsync(
		BackupRunnerRequest request, 
		BackupSessionStateKey sessionKey,
		IProgress<BackupRunnerProgress>? progress,
		CancellationToken cancellationToken);
}
