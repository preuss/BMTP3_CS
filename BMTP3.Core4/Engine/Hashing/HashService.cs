using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Engine.Exceptions;
using BMTP3.Core4.Hashing;
using BMTP3.Core4.Models;

namespace BMTP3.Core4.Engine.Hashing;

internal sealed class HashService : IHashService
{
	private readonly IHashGenerator _hashGenerator;

	public HashService(IHashGenerator hashGenerator)
	{
		_hashGenerator = hashGenerator ?? throw new ArgumentNullException(nameof(hashGenerator));
	}

	public async Task<Dictionary<HashType, string>> ComputeHashesAsync(
		IContent content,
		string relativeFilePath,
		IReadOnlyCollection<HashAlgorithmType> algorithms,
		IProgress<ulong>? progress,
		CancellationToken cancellationToken
	)
	{
		try
		{
			List<HashType> hashTypes = algorithms.Select(ToHashType).ToList();
			await using Stream stream = await content.OpenReadAsync(cancellationToken);
			return await _hashGenerator.ComputeHashesAsync(stream, hashTypes, progress, cancellationToken);
		} catch(OperationCanceledException)
		{
			throw;
		} catch(Exception ex)
		{
			throw new BackupHashException("Hashing failed for backup item.", relativeFilePath, ex);
		}
	}

	private static HashType ToHashType(HashAlgorithmType a) => a switch
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
		_ => throw new ArgumentOutOfRangeException(nameof(a), a, null)
	};
}
