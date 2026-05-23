using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Models;

namespace BMTP3.Core4.Engine.Runner;

internal sealed class ParallelBackupRunner : IBackupRunner
{
	public Task<BackupResultItem> RunAsync(
		BackupItem item,
		string destinationPath,
		string tempFilePath,
		BackupRunnerRequest request,
		CancellationToken cancellationToken)
	{
		throw new NotImplementedException("ParallelBackupRunner is not yet implemented.");
	}
}
