namespace BMTP3.Core4.Hashing;

// Minimal hash generator that returns an empty result set (placeholder).
public class NoopHashGenerator : IHashGenerator
{
	public Task<Dictionary<HashType, string>> ComputeHashesAsync(
		Stream dataStream,
		IEnumerable<HashType> hashTypes,
		IProgress<ulong>? progress,
		CancellationToken cancellationToken)
	{
		Dictionary<HashType, string> empty = new();
		return Task.FromResult(empty);
	}
}