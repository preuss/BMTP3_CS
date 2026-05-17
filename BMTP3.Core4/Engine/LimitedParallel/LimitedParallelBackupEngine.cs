using BMTP3.Core4.Api.Models;

namespace BMTP3.Core4.Engine.LimitedParallel;

internal sealed class LimitedParallelBackupEngine : IBackupRunner
{
	public Task<BackupResult> RunAsync(
		BackupPlan plan,
		IProgress<BackupProgress>? progress,
		CancellationToken cancellationToken)
	{
		throw new NotImplementedException("LimitedParallelBackupEngine is not yet implemented.");
	}
}
