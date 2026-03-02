using BMTP3.Core2.BackupNew.Domain.Item;

namespace BMTP3.Core2.BackupNew.Engine.Strategies;

/// <summary>
/// Responsible for generating sidecar files (metadata files) for backed up items.
/// </summary>
public interface ISidecarGenerator
{
	/// <summary>
	/// Generates a sidecar file for the given item.
	/// </summary>
	/// <param name="item">The item to generate sidecar for.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>True if sidecar was generated, false otherwise.</returns>
	Task<bool> GenerateAsync(IBackupItem item, CancellationToken ct);
}
