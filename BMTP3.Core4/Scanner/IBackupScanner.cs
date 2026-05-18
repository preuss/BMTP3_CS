using BMTP3.Core4.Models;
using BMTP3.Core4.Traversal;

namespace BMTP3.Core4.Scanner;

/// <summary>
///     Discovers backup items by consuming an <see cref="ISourceTraversal" />
///     and mapping <see cref="SourceTraversalItem" /> to <see cref="BackupItem" />.
/// </summary>
internal interface IBackupScanner
{
	/// <summary>
	///     Scans the source via <paramref name="traversal" /> and returns
	///     <see cref="BackupItem" /> instances for the backup pipeline.
	/// </summary>
	IAsyncEnumerable<BackupItem> ScanAsync(
		ISourceTraversal traversal,
		BackupScanRequest request,
		IProgress<BackupScanProgress>? progress,
		CancellationToken cancellationToken);
}
