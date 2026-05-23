using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Models;

namespace BMTP3.Core4.Engine.Runner;

internal sealed class LimitedParallelBackupRunner : IBackupRunner
{
	public Task<BackupResultItem> RunAsync(
		BackupItem item,
		FileInfo destinationFile,
		FileInfo tempFile,
		BackupRunnerRequest request,
		CancellationToken cancellationToken)
	{
		throw new NotImplementedException("LimitedParallelBackupRunner is not yet implemented.");
	}
}
