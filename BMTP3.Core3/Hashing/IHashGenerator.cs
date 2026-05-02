namespace BMTP3.Core3.Hashing;

/// <summary>
/// Stream-based hash generator.
/// Computes multiple hash types in a single pass through the stream.
/// </summary>
public interface IHashGenerator
{
	/// <summary>
	/// Compute multiple hashes from a stream in a single pass.
	/// </summary>
	/// <param name="stream">Source stream to hash.</param>
	/// <param name="hashTypes">Hash algorithms to compute.</param>
	/// <param name="progress">Progress reporter for bytes processed.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>Dictionary mapping HashType to hex string representation.</returns>
	Task<Dictionary<HashType, string>> ComputeHashesAsync(
		Stream stream,
		IEnumerable<HashType> hashTypes,
		IProgress<ulong>? progress,
		CancellationToken ct
	);
}

/// <summary>
/// Item-level hash generator wrapper around IHashGenerator.
/// Opens file and computes hashes.
/// </summary>
public interface IItemHasher
{
	/// <summary>
	/// Compute hashes for a backup item.
	/// </summary>
	/// <param name="item">Item to hash.</param>
	/// <param name="hashTypes">Hash algorithms to compute.</param>
	/// <param name="progress">Progress reporter for bytes processed.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>Dictionary mapping HashType to hex string representation.</returns>
	Task<Dictionary<HashType, string>> ComputeHashesAsync(
		BackupItem item,
		List<HashType> hashTypes,
		IProgress<ulong>? progress,
		CancellationToken ct
	);
}
