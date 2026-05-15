using BMTP3.Core4.Api.Models;
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
	/// <param name="plan">
	/// The backup plan describing the source to scan.
	/// </param>
	/// <param name="cancellationToken">
	/// Token used to observe cancellation requests.
	/// </param>
	/// <returns>
	/// An asynchronous sequence of discovered backup items.
	/// </returns>
	IAsyncEnumerable<BackupItem> ScanAsync(
		BackupPlan plan,
		CancellationToken cancellationToken);
}