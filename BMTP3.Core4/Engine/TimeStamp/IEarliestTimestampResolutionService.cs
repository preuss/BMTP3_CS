using BMTP3.Core4.Engine.TimeStamp.Candidates;
using BMTP3.Core4.Models;

namespace BMTP3.Core4.Engine.TimeStamp;

/// <summary>
///     Holds the result of resolving the earliest valid timestamp from a set of metadata candidates.
/// </summary>
internal sealed record EarliestTimestampResolutionResult
{
	/// <summary>
	///     The earliest valid timestamp found across all metadata sources,
	///     or <c>null</c> if no candidate could be converted to a meaningful date.
	/// </summary>
	public DateTimeOffset? Timestamp { get; init; }

	/// <summary>
	///     The <see cref="TimestampCandidate" /> that produced the earliest timestamp.
	///     Useful for debugging and for identifying the source metadata type (<see cref="TimestampCandidate.SourceType" />).
	/// </summary>
	public TimestampCandidate? Candidate { get; init; }
}

/// <summary>
///     Resolves the earliest valid timestamp from embedded metadata (EXIF, XMP, QuickTime, etc.)
///     and filesystem attributes for a given piece of content.
///     <para>
///         <see cref="ResolveEarliestAsync" /> accepts an <see cref="IContent" /> instance,
///         reads all available metadata directories, extracts timestamp candidates,
///     converts each candidate to a <see cref="DateTimeOffset" /> (assuming UTC for
///     timezone-naive values), filters out dates before the Unix epoch (1970-01-01),
///     and returns the earliest (minimum UTC) value.
///     </para>
/// </summary>
internal interface IEarliestTimestampResolutionService
{
	/// <summary>
	///     Scans all metadata readers for timestamp candidates and returns the earliest
	///     valid <see cref="DateTimeOffset" /> together with the source candidate.
	/// </summary>
	/// <param name="content">
	///     The content to extract timestamps from.
	///     Must implement <see cref="IFileInfoSource" /> with a resolvable <see cref="FileInfo" />.
	///     If the content does not provide file-level access, an empty result is returned.
	/// </param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>
	///     An <see cref="EarliestTimestampResolutionResult" /> containing the earliest timestamp
	///     (or <c>null</c> if none could be resolved) and the candidate that produced it.
	/// </returns>
	Task<EarliestTimestampResolutionResult> ResolveEarliestAsync(
		IContent content,
		CancellationToken cancellationToken);
}
