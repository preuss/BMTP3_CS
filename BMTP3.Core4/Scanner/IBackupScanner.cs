using BMTP3.Core4.Models;

namespace BMTP3.Core4.Scanner;

/// <summary>
/// Defines a scanner that discovers backup items from a configured source.
/// </summary>
internal interface IBackupScanner
{
	/// <summary>
	/// Scans the configured source and returns discovered backup items.
	/// </summary>
	IAsyncEnumerable<BackupScanResult> ScanAsync(
		BackupScanRequest request,
		IProgress<BackupScanProgress>? progress,
		CancellationToken cancellationToken);
}
