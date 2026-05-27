using BMTP3.Core4.Api.Models.Enums;
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

	public async Task<IDictionary<HashAlgorithm, string>> ComputeHashesAsync(
		IContent content,
		IReadOnlyCollection<HashAlgorithm> algorithms,
		IProgress<ulong>? progress,
		CancellationToken cancellationToken
	)
	{
		await using Stream stream = await content.OpenReadStreamAsync(cancellationToken);
		return await _hashGenerator.ComputeHashesAsync(stream, algorithms, progress, cancellationToken);
	}
}
