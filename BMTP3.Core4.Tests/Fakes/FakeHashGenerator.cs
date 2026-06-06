using BMTP3.Core4.Hashing;

namespace BMTP3.Core4.Tests.Fakes;

internal sealed class FakeHashGenerator : IHashGenerator
{
	public Task<Dictionary<HashType, string>> ComputeHashesAsync(
		Stream stream,
		IEnumerable<HashType> hashTypes,
		IProgress<ulong>? progress,
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
