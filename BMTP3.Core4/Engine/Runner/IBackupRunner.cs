using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Models;

namespace BMTP3.Core4.Engine.Runner;

internal interface IBackupRunner
{
	Task<BackupResultItem> RunAsync(
		BackupItem item,
		string destinationPath,
		string tempFilePath,
		BackupRunnerRequest request,
		CancellationToken cancellationToken);
}
