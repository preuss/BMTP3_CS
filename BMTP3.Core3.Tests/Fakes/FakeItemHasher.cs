using BMTP3.Core3.Hashing;

namespace BMTP3.Core3.Tests.Fakes;

/// <summary>
/// Fake hash generator that returns deterministic test hashes.
/// </summary>
public class FakeItemHasher : IItemHasher
{
	public bool ShouldFail { get; set; }

	public Task<Dictionary<HashType, string>> ComputeHashesAsync(
		BackupItem item,
		List<HashType> hashTypes,
		IProgress<ulong>? progress,
		CancellationToken ct = default)
	{
		ct.ThrowIfCancellationRequested();

		if (ShouldFail)
		{
			throw new InvalidOperationException("Fake hash generator configured to fail");
		}

		var hashes = new Dictionary<HashType, string>();
		foreach (var hashType in hashTypes)
		{
			hashes[hashType] = $"fake_{hashType}_{item.Name.GetHashCode():x8}";
		}

		progress?.Report((ulong)item.SizeInBytes);
		return Task.FromResult(hashes);
	}
}
