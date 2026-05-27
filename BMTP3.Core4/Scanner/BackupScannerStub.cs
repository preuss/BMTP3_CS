using BMTP3.Core4.Models;
using BMTP3.Core4.Traversal;

namespace BMTP3.Core4.Scanner;

internal sealed class BackupScannerStub : IBackupScanner
{
	public IAsyncEnumerable<BackupItem> ScanAsync(
		ISourceTraversal traversal,
		BackupScanRequest request,
		IProgress<BackupScanProgress>? progress,
		CancellationToken cancellationToken)
	{
		throw new NotImplementedException("IBackupScanner is not yet implemented.");
	}
}
