using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Engine.Session;

namespace BMTP3.Core4.Engine.Runner;

// TODO: Implement LimitedParallelBackupRunner with a configurable degree of parallelism. Later Tiers: LimitedParallelBackupEngineWithConfig that accepts a configuration object specifying the degree of parallelism and other related settings.
internal sealed class LimitedParallelBackupRunner : IBackupRunner
{
	public Task<BackupResult> RunAsync(
		BackupRunnerRequest request, 
		BackupSessionKey sessionKey, 
		IProgress<BackupRunnerProgress>? progress, 
		CancellationToken cancellationToken
	)
	{
		throw new NotImplementedException("LimitedParallelBackupRunner is not yet implemented.");

	}
}
