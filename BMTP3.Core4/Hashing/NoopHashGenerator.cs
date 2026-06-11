using BMTP3.Core4.Infrastructure.Throttling;

namespace BMTP3.Core4.Hashing;

// Minimal hash generator that returns an empty result set (placeholder).
public class NoopHashGenerator : IHashGenerator
{
	public async Task<Dictionary<HashType, string>> ComputeHashesAsync(
		Stream dataStream,
		IEnumerable<HashType> hashTypes,
		IProgress<ulong>? progress,
		IThrottler throttler,
		CancellationToken cancellationToken
	)

	{
		await throttler.WaitAsync();
		return new Dictionary<HashType, string>();
	}
}