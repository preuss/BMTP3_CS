using BMTP3.Core4.Hashing;
using BMTP3.Core4.Infrastructure.Throttling;

namespace BMTP3.Core4.Tests.Fakes;

internal sealed class FakeHashGenerator : IHashGenerator
{
	public Task<Dictionary<HashType, string>> ComputeHashesAsync(
		Stream stream,
		IEnumerable<HashType> hashTypes,
		IProgress<ulong>? progress,
		IThrottler throttler,
		CancellationToken ct)
	{
		Dictionary<HashType, string> results = new();
		foreach(HashType type in hashTypes.Distinct())
		{
			results[type] = $"fake-{type}";
		}
		return Task.FromResult(results);
	}
}
