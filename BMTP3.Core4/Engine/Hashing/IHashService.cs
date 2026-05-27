using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Hashing;
using BMTP3.Core4.Models;

namespace BMTP3.Core4.Engine.Hashing;

/// <summary>
/// Computes one or more cryptographic hashes for an <see cref="IContent"/> instance.
/// Implementations should stream the content and compute all requested algorithms in a
/// single pass. Implementations may throw IOException, UnauthorizedAccessException or
/// OperationCanceledException on error.
/// </summary>
internal interface IHashService
{
	/// <summary>
	/// Compute the requested hash algorithms for the provided content.
	/// </summary>
	/// <param name="content">Content to compute hashes for (IContent).</param>
	/// <param name="algorithms">The set of algorithms to compute. If empty, no hashes are computed.</param>
	/// <param name="progress">Optional progress callback that reports bytes processed.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A dictionary mapping each requested algorithm to its hex digest string.</returns>
	Task<IDictionary<HashType, string>> ComputeHashesAsync(
		IContent content,
		IReadOnlyCollection<HashAlgorithmType> algorithms,
		IProgress<ulong>? progress,
		CancellationToken cancellationToken
	);
}
