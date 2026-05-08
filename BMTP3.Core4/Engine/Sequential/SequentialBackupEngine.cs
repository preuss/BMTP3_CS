using BMTP3.Core4.Api;
using BMTP3.Core4.Engine.State;
using BMTP3.Core4.Engine.Validation;
using BMTP3.Core4.Models;

public sealed class SequentialBackupEngine : IBackupEngine
{
	public Task<BackupResult> RunAsync(
		BackupPlan plan,
		IProgress<IBackupProgress>? progress,
		CancellationToken cancellationToken)
	{
		// 1. Validate backup plan
		BackupPlanValidator.Validate(plan);

		// 2. Initialize backup state
		BackupExecutionState state = new(plan);

		throw new NotImplementedException();
	}
}
