using BMTP3.Core4.Engine.TimeStamp.Candidates;

namespace BMTP3.Core4.Engine.TimeStamp.Readers;

/// <summary>
///     Extended timestamp reader that collects results from all sub-readers
///     and reports per-reader errors instead of failing fast.
/// </summary>
/// <remarks>
///     <see cref="ITimestampReader.Read"/> is inherited and throws
///     <see cref="TimestampReaderException"/> when <em>all</em> sub-readers fail.
///     Use <see cref="TryReadCollect"/> when you need individual error information
///     and want to distinguish partial success from total failure.
/// </remarks>
internal interface ICompositeTimestampReader : ITimestampReader
{
	/// <summary>
	///     Attempts to read timestamp candidates from all sub-readers.
	///     Individual reader failures are collected and reported via <paramref name="errors"/>
	///     rather than thrown.
	/// </summary>
	/// <param name="fileInfo">The file to read metadata from.</param>
	/// <param name="candidates">All successfully read candidates, or an empty list.</param>
	/// <param name="errors">Per-reader errors wrapped in <see cref="TimestampReaderException"/>.</param>
	/// <param name="cancellationToken">Cancellation token passed to each sub-reader.</param>
	/// <returns>
	///     <c>true</c> if at least one sub-reader produced candidates;
	///     <c>false</c> if all sub-readers failed.
	/// </returns>
	bool TryReadCollect(
		FileInfo fileInfo,
		out IReadOnlyList<TimestampCandidate> candidates,
		out IReadOnlyList<TimestampReaderException> errors,
		CancellationToken cancellationToken
	);
}
