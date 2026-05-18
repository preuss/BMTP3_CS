using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Engine.State;

namespace BMTP3.Core4.Engine.Runner;

// TODO: Implement LimitedParallelBackupRunner with a configurable degree of parallelism. Later Tiers: LimitedParallelBackupEngineWithConfig that accepts a configuration object specifying the degree of parallelism and other related settings.
internal sealed class LimitedParallelBackupRunner : IBackupRunner
{
	public Task<BackupResult> RunAsync(
		BackupRunnerRequest request, 
		BackupSessionStateKey sessionKey, 
		IProgress<BackupRunnerProgress>? progress, 
		CancellationToken cancellationToken
	)
	{
		throw new NotImplementedException("LimitedParallelBackupRunner is not yet implemented.");

	}
}
