using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Engine.Session;

namespace BMTP3.Core4.Engine.Runner;

internal interface IBackupRunner
{
	Task<BackupResult> RunAsync(
		BackupRunnerRequest request, 
		BackupSessionKey sessionKey,
		IProgress<BackupRunnerProgress>? progress,
		CancellationToken cancellationToken);
}
