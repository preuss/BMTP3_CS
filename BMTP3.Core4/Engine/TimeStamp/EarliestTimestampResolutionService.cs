using BMTP3.Core4.Engine.TimeStamp.Candidates;
using BMTP3.Core4.Engine.TimeStamp.Readers;
using BMTP3.Core4.Models;

namespace BMTP3.Core4.Engine.TimeStamp;

/// <summary>
///     Default implementation of <see cref="IEarliestTimestampResolutionService" />.
///     <para>
///         Uses <see cref="CompositeTimestampReader" /> to collect all metadata candidates
///         (filesystem dates, EXIF, XMP, IPTC, GPS, QuickTime),
///         converts each to <see cref="DateTimeOffset" />, filters out dates before the Unix epoch
///         and candidates without a full date (year, month, day), then resolves the earliest using a
///         precision-aware comparison: if two candidates fall on the same calendar day or one day
///         apart, the more precise one wins (DateTimeOffset &#x226B; DateTime &#x226B; Date-only);
///         otherwise the earlier date wins regardless of precision.
///     </para>
/// </summary>
internal sealed class EarliestTimestampResolutionService : IEarliestTimestampResolutionService
{
	/// <summary>
	///     Maximum number of calendar days between two timestamps for them to be considered
	///     "the same event" — allowing the more precise candidate to win over the earlier one.
	///     One day accounts for timezone-induced date shifts in metadata without an offset.
	/// </summary>
	private const int SameDayToleranceDays = 1;

	private static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

	private readonly CompositeTimestampReader _reader;

	public EarliestTimestampResolutionService()
	{
		_reader = new CompositeTimestampReader();
	}

	/// <inheritdoc />
	public Task<EarliestTimestampResolutionResult> ResolveEarliestAsync(
		IContent content,
		CancellationToken cancellationToken
	)
	{
		if(content is not IFileInfoSource fileSource || !fileSource.TryGetFileInfo(out FileInfo file))
		{
			return Task.FromResult(new EarliestTimestampResolutionResult());
		}

		IReadOnlyList<TimestampCandidate> candidates = _reader.Read(file, cancellationToken);

		DateTimeOffset? bestValue = null;
		TimestampCandidate? bestCandidate = null;

		foreach(TimestampCandidate candidate in candidates)
		{
			DateTimeOffset? converted = ConvertToDateTimeOffset(candidate);
			if(converted == null)
			{
				continue;
			}

			// Dates before the Unix epoch are almost certainly corrupt or default metadata values.
			if(converted.Value.UtcDateTime < UnixEpoch)
			{
				continue;
			}

			(bestValue, bestCandidate) = PickBetter(bestValue, bestCandidate, converted.Value, candidate);
		}

		return Task.FromResult(new EarliestTimestampResolutionResult
		{
			Timestamp = bestValue,
			Candidate = bestCandidate,
		});
	}

	/// <summary>
	///     Compares the current best candidate against a challenger and returns the better one.
	///     <para>
	///         "Better" is defined as:
	///         <list type="bullet">
	///             <item>If the two dates are within <see cref="SameDayToleranceDays" /> of each other, the more precise candidate wins.</item>
	///             <item>Otherwise, the earlier (smaller) date wins.</item>
	///         </list>
	///     </para>
	/// </summary>
	private static (DateTimeOffset? Value, TimestampCandidate? Candidate) PickBetter(
		DateTimeOffset? currentValue, TimestampCandidate? currentCandidate,
		DateTimeOffset? challengerValue, TimestampCandidate? challengerCandidate)
	{
		if(currentValue == null)
		{
			return (challengerValue, challengerCandidate);
		}

		if(challengerValue == null)
		{
			return (currentValue, currentCandidate);
		}

		if(WithinDayTolerance(currentValue.Value, challengerValue.Value, SameDayToleranceDays))
		{
			// Same or adjacent day: the more precise candidate wins.
			return IsMorePrecise(currentCandidate, challengerCandidate)
				? (currentValue, currentCandidate)
				: (challengerValue, challengerCandidate);
		}

		// More than one day apart: the earlier date wins.
		return currentValue.Value.UtcTicks <= challengerValue.Value.UtcTicks
			? (currentValue, currentCandidate)
			: (challengerValue, challengerCandidate);
	}

	/// <summary>
	///     Returns <see langword="true" /> if two timestamps are within <paramref name="toleranceDays" />
	///     calendar days of each other (UTC).
	/// </summary>
	private static bool WithinDayTolerance(DateTimeOffset a, DateTimeOffset b, int toleranceDays)
	{
		TimeSpan dayDiff = a.UtcDateTime.Date - b.UtcDateTime.Date;
		return Math.Abs(dayDiff.Days) <= toleranceDays;
	}

	/// <summary>
	///     Returns <see langword="true" /> if <paramref name="candidate" /> is at least as precise
	///     as <paramref name="challenger" />.
	///     <para>
	///         Precision ordering (highest to lowest):
	///         <list type="number">
	///             <item>Date + time + timezone offset (full <see cref="DateTimeOffset" />)</item>
	///             <item>Date + time (no timezone)</item>
	///             <item>Full date only (year, month, day)</item>
	///         </list>
	///     </para>
	/// </summary>
	private static bool IsMorePrecise(TimestampCandidate? candidate, TimestampCandidate? challenger)
	{
		bool candidateHasOffset = candidate?.Offset.HasValue ?? false;
		bool candidateHasTime = candidate?.Time.HasValue ?? false;
		bool challengerHasOffset = challenger?.Offset.HasValue ?? false;
		bool challengerHasTime = challenger?.Time.HasValue ?? false;

		// A candidate with a timezone offset is more precise than one without.
		if(candidateHasOffset != challengerHasOffset)
		{
			return candidateHasOffset;
		}

		// A candidate with a time component is more precise than a date-only candidate.
		if(candidateHasTime != challengerHasTime)
		{
			return candidateHasTime;
		}

		// Both have the same tier — treat as equally precise, keep current.
		return true;
	}

	/// <summary>
	///     Attempts to convert a <see cref="TimestampCandidate" /> to a <see cref="DateTimeOffset" />.
	///     Returns <c>null</c> if the candidate does not have a full date (<see cref="ChronoDateResolution.FullDate" />),
	///     or contains insufficient data (no date, no time, etc.).
	///     <para>
	///         Conversion is attempted in order of specificity:
	///         <list type="number">
	///             <item>
	///                 <b><see cref="TimestampCandidate.TryToDateTimeOffset" /></b> —
	///                 requires <c>Date + Time + Offset</c>. Returns the value with its original offset.
	///             </item>
	///             <item>
	///                 <b><see cref="TimestampCandidate.TryToDateTime" /></b> —
	///                 requires <c>Date + Time</c> (no offset). The resulting <see cref="DateTime" />
	///                 has <see cref="DateTimeKind.Unspecified" />; we assume UTC
	///                 (<c>TimeSpan.Zero</c>) as the convention for metadata without timezone.
	///             </item>
	///             <item>
	///                 <b><see cref="TimestampCandidate.Date" /></b> —
	///                 only a full date is present. Returns midnight UTC
	///                 (<c>Date + 00:00:00 + TimeSpan.Zero</c>).
	///             </item>
	///         </list>
	///     </para>
	/// </summary>
	private static DateTimeOffset? ConvertToDateTimeOffset(TimestampCandidate candidate)
	{
		// Partial dates (year-only or year+month) are too imprecise to be useful.
		if(candidate.DateResolution != ChronoDateResolution.FullDate)
		{
			return null;
		}

		if(candidate.TryToDateTimeOffset(out DateTimeOffset dto))
		{
			return dto;
		}

		if(candidate.TryToDateTime(out DateTime dt))
		{
			return new DateTimeOffset(dt, TimeSpan.Zero);
		}

		if(candidate.Date.HasValue)
		{
			return new DateTimeOffset(candidate.Date.Value, TimeOnly.MinValue, TimeSpan.Zero);
		}

		return null;
	}
}
