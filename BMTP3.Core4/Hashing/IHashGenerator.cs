using BMTP3.Core4.Infrastructure.Throttling;

namespace BMTP3.Core4.Hashing;

public interface IHashGenerator
{
	/// <summary>
	///     Computes multiple hashes from a single stream in one pass.
	/// </summary>
	Task<Dictionary<HashType, string>> ComputeHashesAsync(
		Stream stream,
		IEnumerable<HashType> hashTypes,
		IProgress<ulong>? progress,
		IThrottler throttler,
		CancellationToken cancellationToken
	);
}