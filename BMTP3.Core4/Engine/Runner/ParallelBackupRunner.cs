using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Engine.State;

namespace BMTP3.Core4.Engine.Runner;

// TODO: Implement this class to execute backup items in parallel while respecting dependencies and resource constraints. In a later tier, this will be the default backup engine used by BackupEngine, replacing SequentialBackupRunner.
internal sealed class ParallelBackupRunner : IBackupRunner
{
	public Task<BackupResult> RunAsync(
		BackupRunnerRequest request, 
		BackupSessionStateKey sessionKey, 
		IProgress<BackupRunnerProgress>? progress, 
		CancellationToken cancellationToken
	)
	{
		throw new NotImplementedException("ParallelBackupRunner is not yet implemented.");
	}
}
