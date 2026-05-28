namespace BMTP3.Core4.Engine.DiskSpace;

/// <summary>
/// Validates available disk space for backup operations.
/// </summary>
internal interface IDiskSpaceValidator
{
	/// <summary>
	/// Ensures the destination drive has at least the minimum required free space
	/// for the backup application to function (logs, metadata, temp files).
	/// Throws <see cref="IOException"/> if the minimum threshold is not met.
	/// </summary>
	Task EnsureMinimumFreeSpaceAsync(string destinationPath, CancellationToken cancellationToken);

	/// <summary>
	/// Ensures the destination drive has sufficient free space to accommodate
	/// the total backup size plus a buffer for sidecars and overhead.
	/// Throws <see cref="IOException"/> if capacity is insufficient.
	/// </summary>
	Task EnsureSufficientBackupCapacityAsync(string destinationPath, long totalBytesRequired, CancellationToken cancellationToken);
}
