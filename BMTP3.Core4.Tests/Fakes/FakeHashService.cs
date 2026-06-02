using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Engine.Hashing;
using BMTP3.Core4.Hashing;
using BMTP3.Core4.Models;
using System.Security.Cryptography;
using System.Text;

namespace BMTP3.Core4.Tests.Fakes;

internal sealed class FakeHashService : IHashService
{
	public Task<Dictionary<HashType, string>> ComputeHashesAsync(
		IContent content,
		string relativePath,
		IReadOnlyCollection<HashAlgorithmType> algorithms,
		IProgress<ulong>? progress,
		CancellationToken ct)
	{
		Dictionary<HashType, string> results = new();
		foreach (HashAlgorithmType algorithm in algorithms)
		{
			string hex = Convert.ToHexString(
				SHA256.HashData(Encoding.UTF8.GetBytes("fake-data-" + algorithm))
			).ToLowerInvariant();
			results[MapToHashType(algorithm)] = hex;
		}
		return Task.FromResult(results);
	}

	private static HashType MapToHashType(HashAlgorithmType algorithm) => algorithm switch
	{
		HashAlgorithmType.MD5_128 => HashType.MD5_128,
		HashAlgorithmType.SHA2_256 => HashType.SHA2_256,
		HashAlgorithmType.SHA2_512 => HashType.SHA2_512,
		HashAlgorithmType.SHA3_256_FIPS202 => HashType.SHA3_256_FIPS202,
		HashAlgorithmType.SHA3_512_FIPS202 => HashType.SHA3_512_FIPS202,
		HashAlgorithmType.SHA3_256_KECCAK => HashType.SHA3_256_KECCAK,
		HashAlgorithmType.SHA3_512_KECCAK => HashType.SHA3_512_KECCAK,
		HashAlgorithmType.BLAKE3_256 => HashType.BLAKE3_256,
		HashAlgorithmType.BLAKE3_512 => HashType.BLAKE3_512,
		_ => HashType.SHA2_256,
	};
}
