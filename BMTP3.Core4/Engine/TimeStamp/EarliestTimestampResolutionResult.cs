using BMTP3.Core4.Engine.TimeStamp.Candidates;

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