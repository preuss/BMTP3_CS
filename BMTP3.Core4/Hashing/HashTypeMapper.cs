using BMTP3.Core4.Api.Models.Enums;

namespace BMTP3.Core4.Hashing;

internal static class HashTypeMapper
{
	public static HashType ToHashType(HashAlgorithmType algorithmType) => algorithmType switch
	{
		HashAlgorithmType.SHA2_256 => HashType.SHA2_256,
		HashAlgorithmType.SHA2_512 => HashType.SHA2_512,
		HashAlgorithmType.SHA3_256_FIPS202 => HashType.SHA3_256_FIPS202,
		HashAlgorithmType.SHA3_512_FIPS202 => HashType.SHA3_512_FIPS202,
		HashAlgorithmType.SHA3_256_KECCAK => HashType.SHA3_256_KECCAK,
		HashAlgorithmType.SHA3_512_KECCAK => HashType.SHA3_512_KECCAK,
		HashAlgorithmType.MD5_128 => HashType.MD5_128,
		HashAlgorithmType.BLAKE3_256 => HashType.BLAKE3_256,
		HashAlgorithmType.BLAKE3_512 => HashType.BLAKE3_512,
		_ => throw new ArgumentOutOfRangeException(nameof(algorithmType), algorithmType, null),
	};
}