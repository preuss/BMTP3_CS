using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Engine.Session;

namespace BMTP3.Core4.Engine.Runner;

internal sealed class SequentialBackupRunner : IBackupRunner
{
	public Task<BackupResult> RunAsync(
		BackupRunnerRequest request,
		BackupSessionKey sessionKey,
		IProgress<BackupRunnerProgress>? progress,
		CancellationToken cancellationToken
	)
	{
		throw new NotImplementedException("SequentialBackupRunner is not yet implemented.");
	}
}
