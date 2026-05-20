namespace BMTP3.Core4.Api.Models.Enums;

/// <summary>
/// Hash algorithm types used for file integrity verification.
/// All algorithms are computed in a single pass when multiple are selected.
/// </summary>
public enum HashAlgorithm
{
	SHA2_256,
	SHA2_512,
	SHA3_256_FIPS202,
	SHA3_512_FIPS202,
	SHA3_256_KECCAK,
	SHA3_512_KECCAK,
	MD5_128,
	BLAKE3_256,
	BLAKE3_512
}
