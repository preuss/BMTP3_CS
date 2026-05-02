namespace BMTP3.Core3.Sidecar;

/// <summary>
/// Interface for sidecar file generation.
/// Sidecar files contain metadata and hash information for backup verification.
/// </summary>
public interface ISidecarGenerator
{
	/// <summary>
	/// Generate a sidecar file for a backed-up item.
	/// </summary>
	/// <param name="item">Backup item with metadata and hashes.</param>
	/// <param name="destinationDirectory">Directory where sidecar will be written.</param>
	/// <param name="ct">Cancellation token.</param>
	Task GenerateAsync(BackupItem item, string destinationDirectory, CancellationToken ct);
}
