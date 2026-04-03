using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Domain.Item;

namespace BMTP3.Core2.BackupNew.Engine.Strategies;

public interface IItemHasher
{
	/// <summary>
	/// Computes the requested hashes for the given item.
	/// </summary>
	/// <param name="item">The item to hash (must have openable content).</param>
	/// <param name="hashTypes">List of hash algorithms to compute.</param>
	/// <param name="progress">Optional reporter for bytes processed.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>A dictionary mapping HashType to the hex string representation of the hash.</returns>
	Task<Dictionary<HashType, string>> ComputeHashesAsync(
		IBackupItem item,
		List<HashType> hashTypes,
		IProgress<ulong> progress,
		CancellationToken ct);
}
