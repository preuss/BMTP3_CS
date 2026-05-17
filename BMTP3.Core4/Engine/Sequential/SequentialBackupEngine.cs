using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Engine.State;

namespace BMTP3.Core4.Engine.Sequential;

internal sealed class SequentialBackupEngine : IBackupRunner
{
	public Task<BackupResult> RunAsync(
		BackupSessionStateKey sessionKey,
		IProgress<BackupProgress>? progress,
		CancellationToken cancellationToken)
	{
		throw new NotImplementedException("SequentialBackupEngine is not yet implemented.");
	}
}
