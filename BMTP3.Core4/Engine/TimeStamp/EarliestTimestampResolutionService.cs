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
///     <para>
///         After resolution, the service optionally applies the timestamp to the target file's
///         filesystem attributes and updates item date properties when
///         <paramref name="enableTimestampCorrection" /> is <c>true</c>.
///         Metadata is always updated when a timestamp is resolved.
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

	private readonly ICompositeTimestampReader _compositeTimestampReader;

	public EarliestTimestampResolutionService()
		: this(new CompositeTimestampReader())
	{
	}

	public EarliestTimestampResolutionService(ICompositeTimestampReader reader)
	{
		_compositeTimestampReader = reader ?? throw new ArgumentNullException(nameof(reader));
	}

	/// <inheritdoc />
	public Task<EarliestTimestampResolutionResult> ResolveAndApplyEarliestAsync(
		EarliestTimestampResolutionRequest request,
		bool enableTimestampCorrection,
		CancellationToken cancellationToken
	)
	{
		if(request.Content is not IFileInfoSource fileSource)
		{
			throw new InvalidOperationException($"Content must implement {nameof(IFileInfoSource)}. Actual type: {request.Content?.GetType().FullName ?? "null"}");
		}

		if(!fileSource.TryGetFileInfo(out FileInfo file))
		{
			throw new InvalidOperationException($"IFileInfoSource implementation did not provide a valid FileInfo. Actual type: {fileSource.GetType().FullName}");
		}

		// The real metadata extraction.
		IReadOnlyList<TimestampCandidate> candidates = _compositeTimestampReader.Read(file, cancellationToken);

		EarliestTimestampResolutionResult best = candidates
			.Select(ResolveCandidate)
			.Where(HasValidDate)
			.Aggregate(
				new EarliestTimestampResolutionResult(),
				PickBetter
			);

		if(best.Timestamp.HasValue)
		{
			request.Metadata.ResolvedDateTime = best.Timestamp;

			if(enableTimestampCorrection)
			{
				BackupItem item = request.Item;

				// Only date not in filesystem - We assume that "authored" corresponds to the most meaningful timestamp for the content, and that filesystem dates should be aligned to it.
				item.DateAuthored = best.Timestamp;

				// Same as filesystem dates - we set them all to the same value for consistency, as we cannot be sure which one (created, modified, accessed) is more "correct" without additional context. This also simplifies the logic and avoids confusion from having different filesystem dates that are close but not identical.
				item.DateCreated = best.Timestamp;
				item.DateModified = best.Timestamp;
				item.DateAccessed = best.Timestamp;

				// Update filesystem timestamps to match the resolved timestamp. We use the same value for all three to avoid confusion and maintain consistency.
				request.TimestampCorrectionTarget.CreationTimeUtc = item.DateCreated.Value.UtcDateTime;
				request.TimestampCorrectionTarget.LastWriteTimeUtc = item.DateModified.Value.UtcDateTime;
				request.TimestampCorrectionTarget.LastAccessTimeUtc = item.DateAccessed.Value.UtcDateTime;
			}
		}

		return Task.FromResult(best);
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
	private static EarliestTimestampResolutionResult PickBetter(
		EarliestTimestampResolutionResult? current,
		EarliestTimestampResolutionResult? challenger
	)
	{
		bool currentValid = HasValidDate(current);
		bool challengerValid = HasValidDate(challenger);

		if(!currentValid && !challengerValid) return new EarliestTimestampResolutionResult();

		if(!currentValid) return challenger!;

		if(!challengerValid) return current!;


		EarliestTimestampResolutionResult currentResult = current!;
		EarliestTimestampResolutionResult challengerResult = challenger!;

		DateTimeOffset currentTimestamp = currentResult.Timestamp!.Value;
		DateTimeOffset challengerTimestamp = challengerResult.Timestamp!.Value;


		if(IsWithinSameEventWindow(currentTimestamp, challengerTimestamp))
		{
			// Same or adjacent day: the more precise candidate wins.
			return MostPrecise(currentResult, challengerResult);
		}

		// More than one day apart: the earlier date wins.
		if(currentTimestamp.UtcTicks <= challengerTimestamp.UtcTicks)
		{
			return currentResult;
		}

		return challengerResult;
	}

	/// <summary>
	///     Returns <see langword="true" /> if two timestamps are within <paramref name="toleranceDays" />
	///     calendar days of each other (UTC).
	/// </summary>
	private static bool IsWithinSameEventWindow(DateTimeOffset first, DateTimeOffset second)
	{
		int dayDifference = GetUtcCalendarDayDifference(first, second);
		return dayDifference <= SameDayToleranceDays;
	}

	/// <summary>
	///     Calculates the absolute difference in calendar days between two <see cref="DateTimeOffset" /> values
	///     using their UTC dates.
	/// </summary>
	private static int GetUtcCalendarDayDifference(DateTimeOffset first, DateTimeOffset second)
	{
		DateOnly firstDate = DateOnly.FromDateTime(first.UtcDateTime);
		DateOnly secondDate = DateOnly.FromDateTime(second.UtcDateTime);

		return Math.Abs(firstDate.DayNumber - secondDate.DayNumber);
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
	private static EarliestTimestampResolutionResult MostPrecise(
		EarliestTimestampResolutionResult current,
		EarliestTimestampResolutionResult challenger
	)
	{
		if(current == null)
		{
			throw new ArgumentNullException(nameof(current), "Current result must not be null.");
		}
		if(challenger == null)
		{
			throw new ArgumentNullException(nameof(challenger), "Challenger result must not be null.");
		}
		if(current.Candidate == null)
		{
			throw new ArgumentException("Current result must have a non-null Candidate.", nameof(current));
		}
		if(challenger.Candidate == null)
		{
			throw new ArgumentException("Challenger result must have a non-null Candidate.", nameof(challenger));
		}

		bool currentHasOffset = current.Candidate.Offset.HasValue;
		bool currentHasTime = current.Candidate.Time.HasValue;
		bool challengerHasOffset = challenger.Candidate.Offset.HasValue;
		bool challengerHasTime = challenger.Candidate.Time.HasValue;

		// A candidate with a timezone offset is more precise than one without.
		if(currentHasOffset != challengerHasOffset)
		{
			return currentHasOffset ? current : challenger;
		}

		// A candidate with a time component is more precise than a date-only candidate.
		if(currentHasTime != challengerHasTime)
		{
			return currentHasTime ? current : challenger;
		}

		// Both have the same tier — return current (arbitrary, but deterministic).
		return current;
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
	private static EarliestTimestampResolutionResult? ResolveCandidate(TimestampCandidate candidate)
	{
		if(!TryConvertToDateTimeOffset(candidate, out DateTimeOffset dto))
			return null;

		return new EarliestTimestampResolutionResult
		{
			Timestamp = dto,
			Candidate = candidate,
		};
	}


	/// <summary>
	///     Attempts to convert a <see cref="TimestampCandidate" /> to a <see cref="DateTimeOffset" />.
	///     Returns <c>false</c> if the candidate does not have a full date, or if conversion fails.
	///     <para>
	///         Conversion is attempted in order of specificity:
	///         <list type="number">
	///             <item><see cref="TimestampCandidate.TryToDateTimeOffset" /> — requires Date + Time + Offset.</item>
	///             <item><see cref="TimestampCandidate.TryToDateTime" /> — requires Date + Time (assumed UTC).</item>
	///             <item><see cref="TimestampCandidate.Date" /> — date-only, returns midnight UTC.</item>
	///         </list>
	///     </para>
	/// </summary>
	private static bool TryConvertToDateTimeOffset(TimestampCandidate candidate, out DateTimeOffset dto)
	{
		dto = default;

		if(candidate.DateResolution != ChronoDateResolution.FullDate)
		{
			return false;
		}

		if(candidate.TryToDateTimeOffset(out dto))
		{
			return true;
		}

		if(candidate.TryToDateTime(out DateTime dt))
		{
			dto = new DateTimeOffset(dt, TimeSpan.Zero);
			return true;
		}

		if(candidate.Date.HasValue)
		{
			dto = new DateTimeOffset(candidate.Date.Value, TimeOnly.MinValue, TimeSpan.Zero);
			return true;
		}
		return false;
	}

	/// <summary>
	///     Returns <c>true</c> if the candidate has a non-null <see cref="EarliestTimestampResolutionResult.Timestamp" />
	///     that is later than the Unix epoch (1970-01-01).
	///     Dates before the epoch are almost certainly corrupt or default metadata values.
	/// </summary>
	private static bool HasValidDate(EarliestTimestampResolutionResult? resultCandidate)
	{
		return resultCandidate?.Timestamp != null && resultCandidate.Timestamp.Value.UtcDateTime > UnixEpoch;
	}
}
