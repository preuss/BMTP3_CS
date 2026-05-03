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
	/// <param name="collisionStrategy">Collision strategy used when destination file already exists.</param>
	/// <param name="progress">Progress reporter for bytes transferred.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>Resolved destination path of the copied file.</returns>
	Task<string> CopyAsync(
		BackupItem item,
		string destinationDirectory,
		CollisionStrategy collisionStrategy,
		IProgress<long>? progress,
		CancellationToken ct
	);
}
