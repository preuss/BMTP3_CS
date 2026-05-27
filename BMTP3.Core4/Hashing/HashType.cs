namespace BMTP3.Core4.Hashing;

/// <summary>
/// Hash algorithm types used for file integrity verification.
/// All algorithms are computed in a single pass when multiple are selected.
/// </summary>
public enum HashType
{
	SHA3_512_FIPS202,
	SHA3_256_FIPS202,
	SHA3_512_KECCAK,
	SHA3_256_KECCAK,
	SHA2_256,
	SHA2_512,
	MD5_128,
	BLAKE3_256,
	BLAKE3_512
}