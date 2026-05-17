using BMTP3.Core4.Api.Models;

namespace BMTP3.Core4.Engine;

internal interface IBackupRunner
{
	Task<BackupResult> RunAsync(
		BackupPlan plan,
		IProgress<BackupProgress>? progress,
		CancellationToken cancellationToken);
}
