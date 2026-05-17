using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Engine.State;

namespace BMTP3.Core4.Engine.LimitedParallel;

// TODO: Implement LimitedParallelBackupEngine with a configurable degree of parallelism. Later Tiers: LimitedParallelBackupEngineWithConfig that accepts a configuration object specifying the degree of parallelism and other related settings.
internal sealed class LimitedParallelBackupEngine// : IBackupRunner
{
	public Task<BackupResult> RunAsync(
		BackupSessionStateKey sessionKey,
		IProgress<BackupProgress>? progress,
		CancellationToken cancellationToken)
	{
		throw new NotImplementedException("LimitedParallelBackupEngine is not yet implemented.");
	}
}
