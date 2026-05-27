namespace BMTP3.Core4.Hashing;

// Minimal hash generator that returns an empty result set (placeholder).
public class NoopHashGenerator : IHashGenerator
{
	public Task<IReadOnlyDictionary<HashType, string>> ComputeHashesAsync(
		Stream dataStream,
		IEnumerable<HashType> hashTypes,
		IProgress<ulong>? progress,
		CancellationToken cancellationToken)
	{
		return Task.FromResult<IReadOnlyDictionary<HashType, string>>(new Dictionary<HashType, string>());
	}
}