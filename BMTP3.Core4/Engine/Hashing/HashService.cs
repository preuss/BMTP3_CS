using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Engine.Exceptions;
using BMTP3.Core4.Hashing;
using BMTP3.Core4.Infrastructure.Throttling;
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
		IThrottler throttler,
		CancellationToken cancellationToken
	)
	{
		try
		{
			List<HashType> hashTypes = algorithms.Select(ToHashType).ToList();
			await using Stream stream = await content.OpenReadAsync(cancellationToken);
			return await _hashGenerator.ComputeHashesAsync(stream, hashTypes, progress, throttler, cancellationToken);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			throw new BackupHashException("Hashing failed for backup item.", relativeFilePath, ex);
		}
	}

	private static HashType ToHashType(HashAlgorithmType a) => HashTypeMapper.ToHashType(a);
}
