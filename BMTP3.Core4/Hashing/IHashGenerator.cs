using BMTP3.Core4.Api.Models.Enums;

namespace BMTP3.Core4.Hashing;

public interface IHashGenerator
{
	Task<Dictionary<HashAlgorithm, string>> ComputeHashesAsync(
		Stream stream,
		IEnumerable<HashAlgorithm> hashTypes,
		IProgress<ulong>? progress,
		CancellationToken ct
	);
}
