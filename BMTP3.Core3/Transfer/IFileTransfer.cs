namespace BMTP3.Core3.Transfer;

/// <summary>
/// Interface for file transfer operations.
/// Handles copying files from source to destination, with progress reporting.
/// </summary>
public interface IFileTransfer
{
	/// <summary>
	/// Copy a file from source to destination.
	/// </summary>
	/// <param name="item">Item containing source path and destination info.</param>
	/// <param name="destinationDirectory">Directory where file will be copied.</param>
	/// <param name="progress">Progress reporter for bytes transferred.</param>
	/// <param name="ct">Cancellation token.</param>
	Task CopyAsync(BackupItem item, string destinationDirectory, IProgress<long>? progress, CancellationToken ct);
}
