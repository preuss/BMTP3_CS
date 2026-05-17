using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Engine.State;

namespace BMTP3.Core4.Engine;

internal interface IBackupRunner
{
	Task<BackupResult> RunAsync(
		BackupSessionStateKey sessionKey,
		IProgress<BackupProgress>? progress,
		CancellationToken cancellationToken);
}
