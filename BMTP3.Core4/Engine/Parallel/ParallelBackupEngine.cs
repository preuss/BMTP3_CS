using BMTP3.Core4.Api.Models;

namespace BMTP3.Core4.Engine.Parallel;

internal sealed class ParallelBackupEngine : IBackupRunner
{
	public Task<BackupResult> RunAsync(
		BackupPlan plan,
		IProgress<BackupProgress>? progress,
		CancellationToken cancellationToken)
	{
		throw new NotImplementedException("ParallelBackupEngine is not yet implemented.");
	}
}
