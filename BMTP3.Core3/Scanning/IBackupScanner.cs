namespace BMTP3.Core3.Scanning;

/// <summary>
/// Scanner for traversing a backup source (filesystem, device, etc.).
/// Returns items (files/folders) to be backed up.
/// </summary>
public interface IBackupScanner
{
	/// <summary>
	/// Scan the source and return all items to backup.
	/// </summary>
	/// <param name="source">Source path (filesystem directory or device identifier).</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>Enumerable of items found in source.</returns>
	Task<IEnumerable<BackupItem>> ScanAsync(string source, CancellationToken ct);
}
