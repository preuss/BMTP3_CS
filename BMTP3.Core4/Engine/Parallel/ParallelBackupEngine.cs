using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Engine.State;

namespace BMTP3.Core4.Engine.Parallel;

// TODO: Implement this class to execute backup items in parallel while respecting dependencies and resource constraints. In a later tier, this will be the default backup engine used by BackupEngine, replacing SequentialBackupEngine.
internal sealed class ParallelBackupEngine// : IBackupRunner
{
	public Task<BackupResult> RunAsync(
		BackupSessionStateKey sessionKey,
		IProgress<BackupProgress>? progress,
		CancellationToken cancellationToken)
	{
		throw new NotImplementedException("ParallelBackupEngine is not yet implemented.");
	}
}
