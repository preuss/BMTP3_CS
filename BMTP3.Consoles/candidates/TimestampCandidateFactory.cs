using BMTP3.Consoles.exifreader.definitions;
using BMTP3.Consoles.exifreader.parsers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Consoles.candidates;
public static class TimestampCandidateFactory
{
	private const long TicksPerSecond = TimeSpan.TicksPerSecond; // 10_000_000
	public static TimeSpan? OffsetToTimeSpan(DateTime dt)
	{
		TimeSpan? offset = dt.Kind switch
		{
			DateTimeKind.Utc => TimeSpan.Zero,
			DateTimeKind.Local => TimeZoneInfo.Local.GetUtcOffset(dt),
			_ => null
		};
		return offset;
	}
	public static TimeSpan? OffsetToTimeSpan(DateTimeOffset dto)
	{
		return dto.Offset;
	}
	public static long? TrimToNanoseconds(long? ticks)
	{
		if(!ticks.HasValue) return null;

		long nanosecondsFractions = (ticks.Value % TicksPerSecond) * 100;
		return nanosecondsFractions == 0 ? null : nanosecondsFractions;
	}
	public static long? TrimToNanoseconds(TimeOnly? time)
	{
		return TrimToNanoseconds(time?.Ticks);
	}
	public static long? TrimToNanoseconds(DateTime? dt)
	{
		return TrimToNanoseconds(dt?.Ticks);
	}
	public static long? TrimToNanoseconds(DateTimeOffset? dto)
	{
		return TrimToNanoseconds(dto?.Ticks);
	}

	public static TimestampCandidate FromDateTime(
		TimestampSourceType sourceType,
		TimestampRole role,
		DateTime dt
	)
	{
		TimeSpan? offset = OffsetToTimeSpan(dt);

		long? nanosecondFractions = TrimToNanoseconds(dt);

		TimeOnly timeWithoutFractions = new(dt.Hour, dt.Minute, dt.Second);

		return new(
			sourceType: sourceType,
			role: role,
			// DateTime has no original source strings
			sources: TimestampSources.Empty,
			date: DateOnly.FromDateTime(dt),
			dateResolution: ChronoDateResolution.FullDate,
			time: timeWithoutFractions,
			subSeconds: nanosecondFractions,
			offset: offset
		);
	}

	public static TimestampCandidate FromDateTimeOffset(
		TimestampSourceType sourceType,
		TimestampRole role,
		DateTimeOffset dto
	)
	{
		TimeSpan? offset = OffsetToTimeSpan(dto);
		long? nanosecondFractions = TrimToNanoseconds(dto);

		TimeOnly time = new(dto.Hour, dto.Minute, dto.Second);

		return new(
			sourceType: sourceType,
			role: role,
			// DateTimeOffset has no original source strings
			sources: TimestampSources.Empty,
			date: DateOnly.FromDateTime(dto.DateTime),
			dateResolution: ChronoDateResolution.FullDate,
			time: time,
			subSeconds: nanosecondFractions,
			offset: offset
		);
	}

	/// <summary>
	/// Creates a candidate from UTC date and time parts.
	/// Automatically sets Offset to Zero and Resolution to FullDate.
	/// </summary>
	public static TimestampCandidate FromUtcDateAndTime(
		TimestampSourceType sourceType,
		TimestampRole role,
		string? rawDate,
		string? rawTime,
		DateOnly? date,
		TimeOnly? time
	)
	{
		long? nanosecondFractions = TrimToNanoseconds(time);
		return new TimestampCandidate(
			sourceType: sourceType,
			role: role,
			sources: new TimestampSources { Date = rawDate, Time = rawTime },
			date: date,
			dateResolution: ChronoDateResolution.FullDate,
			time: time,
			subSeconds: null,
			offset: TimeSpan.Zero
		);
	}

	public static DateTimeOffset ConvertTimestampUtcToDateTimeOffset(
		long timestampUtc, 
		EpochType epoch, 
		TimestampResolution resolution
	)
	{
		// Determine the Base Epoch Date
		DateTimeOffset baseDate = epoch switch
		{
			EpochType.Unix => DateTimeOffset.UnixEpoch,
			EpochType.MacLegacy => new DateTimeOffset(1904, 1, 1, 0, 0, 0, TimeSpan.Zero),
			EpochType.MacCocoa => new DateTimeOffset(2001, 1, 1, 0, 0, 0, TimeSpan.Zero),
			EpochType.DotNetTicks => new DateTimeOffset(0, TimeSpan.Zero), // Year 0001
			EpochType.WindowsFileTime => new DateTimeOffset(1601, 1, 1, 0, 0, 0, TimeSpan.Zero),
			EpochType.GPS => new DateTimeOffset(1980, 1, 6, 0, 0, 0, TimeSpan.Zero),
			EpochType.NTP_Timestamp => new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.Zero),
			EpochType.FatDos => new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero),
			_ => throw new ArgumentOutOfRangeException(nameof(epoch), $"Unsupported epoch type: {epoch}")
		};

		// Add the value based on the specified Resolution
		DateTimeOffset dto = resolution switch
		{
			TimestampResolution.Seconds => baseDate.AddSeconds(timestampUtc),
			TimestampResolution.Milliseconds => baseDate.AddMilliseconds(timestampUtc),
			TimestampResolution.Microseconds => baseDate.AddTicks(timestampUtc * 10),
			TimestampResolution.Ticks100Ns => baseDate.AddTicks(timestampUtc),
			TimestampResolution.Nanoseconds => baseDate.AddTicks(timestampUtc / 100),
			_ => throw new ArgumentOutOfRangeException(nameof(resolution), $"Unsupported resolution: {resolution}")
		};
		return dto;
	}

	/// <summary>
	/// We expect a timestamp to be represented as a raw numeric value (e.g. from Exif or filesystem) along with metadata about how to interpret it (epoch and resolution).
	/// And in UTC, so we can directly convert it to a DateTimeOffset and extract all the components for the candidate. We also keep the original raw timestamp string for reference.
	/// </summary>
	/// <param name="sourceType"></param>
	/// <param name="role"></param>
	/// <param name="rawTimestamp"></param>
	/// <param name="timestampUtc"></param>
	/// <param name="epoch"></param>
	/// <param name="resolution"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	public static TimestampCandidate FromRawTimestamp(
		TimestampSourceType sourceType,
		TimestampRole role,
		string? rawTimestamp,
		long timestampUtc,
		EpochType epoch,
		TimestampResolution resolution
	)
	{
		DateTimeOffset dto = ConvertTimestampUtcToDateTimeOffset(timestampUtc, epoch, resolution);

		TimestampSources sources = new() { Timestamp = rawTimestamp };

		// Standard extraction logic
		TimeSpan? offset = OffsetToTimeSpan(dto);
		long? nanosecondFractions = TrimToNanoseconds(dto);
		TimeOnly time = new(dto.Hour, dto.Minute, dto.Second);

		return new TimestampCandidate(
			sourceType: sourceType,
			role: role,
			sources: sources,
			date: DateOnly.FromDateTime(dto.DateTime),
			dateResolution: ChronoDateResolution.FullDate,
			time: time,
			subSeconds: nanosecondFractions,
			offset: offset
		);
	}

	public static TimestampCandidate FromRawDateTimeOffset(
		TimestampSourceType sourceType,
		TimestampRole role,
		string rawDateTimeOffset,
		DateTimeOffset dto
	)
	{
		TimestampSources sources = new TimestampSources() { DateTimeOffset = rawDateTimeOffset };
		if(sources.IsEmpty)
		{
			throw new ArgumentException("The rawDateTimeOffset cannot be null or whitespace.", nameof(rawDateTimeOffset));
		}

		TimeSpan? offset = OffsetToTimeSpan(dto);
		long? nanosecondFractions = TrimToNanoseconds(dto);
		TimeOnly time = new(dto.Hour, dto.Minute, dto.Second);
		return new TimestampCandidate(
			sourceType: sourceType,
			role: role,
			sources: new TimestampSources { DateTimeOffset = rawDateTimeOffset },
			date: DateOnly.FromDateTime(dto.DateTime),
			dateResolution: ChronoDateResolution.FullDate,
			time: time,
			subSeconds: nanosecondFractions,
			offset: offset
		);
	}

	public static TimestampCandidate FromRawDateTime(
		TimestampSourceType sourceType,
		TimestampRole role,
		string rawDateTime,
		DateTime dt
	)
	{
		TimestampSources sources = new TimestampSources() { DateTime = rawDateTime };
		if(sources.IsEmpty)
		{
			throw new ArgumentException("The rawDateTime cannot be null or whitespace.", nameof(rawDateTime));
		}

		TimeSpan? offset = OffsetToTimeSpan(dt);
		long? nanosecondFractions = TrimToNanoseconds(dt);
		TimeOnly timeWithoutFractions = new(dt.Hour, dt.Minute, dt.Second);
		DateOnly dateOnly = DateOnly.FromDateTime(dt);

		return new TimestampCandidate(
			sourceType: sourceType,
			role: role,
			sources: sources,
			date: dateOnly,
			dateResolution: ChronoDateResolution.FullDate,
			time: timeWithoutFractions,
			subSeconds: nanosecondFractions,
			offset: offset
		);
	}

	public static TimestampCandidate FromRawDate(
		TimestampSourceType sourceType,
		TimestampRole role,
		string rawDate,
		DateOnly date,
		ChronoDateResolution dateResolution
	)
	{
		TimestampSources sources = new TimestampSources() { Date = rawDate };
		if(sources.IsEmpty)
		{
			throw new ArgumentException("The rawDate cannot be null or whitespace.", nameof(rawDate));
		}
		return new TimestampCandidate(
			sourceType: sourceType,
			role: role,
			sources: sources,
			date: date,
			dateResolution: dateResolution,
			time: null,
			subSeconds: null,
			offset: null
		);
	}

	public static TimestampCandidate FromRawTime(
		TimestampSourceType sourceType,
		TimestampRole role,
		string rawTime,
		TimeOnly time
	)
	{
		TimestampSources sources = new TimestampSources() { Time = rawTime };
		if(sources.IsEmpty)
		{
			throw new ArgumentException("The rawTime cannot be null or whitespace.", nameof(rawTime));
		}

		long? subSeconds = TrimToNanoseconds(time);

		// Strip sub-second precision for candidate
		time = new TimeOnly(time.Hour, time.Minute, time.Second);

		return new TimestampCandidate(
			sourceType: sourceType,
			role: role,
			sources: sources,
			date: null,
			dateResolution: null,
			time: time,
			subSeconds: subSeconds,
			offset: null
		);
	}

	public static TimestampCandidate FromRawOffset(
		TimestampSourceType sourceType,
		TimestampRole role,
		string rawOffset,
		TimeSpan offset
	)
	{
		TimestampSources sources = new TimestampSources() { Offset = rawOffset };
		if(sources.IsEmpty)
		{
			throw new ArgumentException("The rawOffset cannot be null or whitespace.", nameof(rawOffset));
		}
		return new TimestampCandidate(
			sourceType: sourceType,
			role: role,
			sources: sources,
			date: null,
			dateResolution: null,
			time: null,
			subSeconds: null,
			offset: offset
		);
	}

	public static TimestampCandidate FromRawSubSeconds(
		TimestampSourceType sourceType,
		TimestampRole role,
		string rawSubSeconds,
		long subSeconds
	)
	{
		TimestampSources sources = new TimestampSources() { SubSeconds = rawSubSeconds };
		if(sources.IsEmpty)
		{
			throw new ArgumentException("The rawSubSeconds cannot be null or whitespace.", nameof(rawSubSeconds));
		}
		return new TimestampCandidate(
			sourceType: sourceType,
			role: role,
			sources: sources,
			date: null,
			dateResolution: null,
			time: null,
			subSeconds: subSeconds,
			offset: null
		);
	}

	public static TimestampCandidate FromParts(
		TimestampSourceType sourceType,
		TimestampRole role,
		TimestampSources? sources,
		DateOnly? date,
		ChronoDateResolution? dateResolution,
		TimeOnly? time,
		long? subSeconds,
		TimeSpan? offset
	)
	{
		// Normalize nullable/empty sources
		sources = TimestampSources.Normalize(sources);

		// date and dateResolution must come together
		if(date.HasValue != dateResolution.HasValue)
		{
			throw new ArgumentException("date and dateResolution must either both be provided or both null.");
		}

		// Predicates for available source strings (consider DateTime/DateTimeOffset as containing both date and time)
		bool hasDateSource = !string.IsNullOrWhiteSpace(sources.Date) ||
							 !string.IsNullOrWhiteSpace(sources.DateTime) ||
							 !string.IsNullOrWhiteSpace(sources.DateTimeOffset);

		bool hasTimeSource = !string.IsNullOrWhiteSpace(sources.Time) ||
							 !string.IsNullOrWhiteSpace(sources.DateTime) ||
							 !string.IsNullOrWhiteSpace(sources.DateTimeOffset);

		bool hasSubSecondsSource = !string.IsNullOrWhiteSpace(sources.SubSeconds) ||
								   hasTimeSource;

		bool hasOffsetSource = !string.IsNullOrWhiteSpace(sources.Offset) ||
							   !string.IsNullOrWhiteSpace(sources.DateTimeOffset);

		// Validate presence of corresponding source strings when values are provided
		if(date.HasValue && !hasDateSource)
		{
			throw new ArgumentException("When 'date' is provided, one of sources.Date, sources.DateTime or sources.DateTimeOffset must be non-empty.", nameof(sources));
		}
		if(time.HasValue && !hasTimeSource)
		{
			throw new ArgumentException("When 'time' is provided, one of sources.Time, sources.DateTime or sources.DateTimeOffset must be non-empty.", nameof(sources));
		}
		if(subSeconds.HasValue && !hasSubSecondsSource)
		{
			throw new ArgumentException("When 'subSeconds' is provided, one of sources.SubSeconds, sources.Time, sources.DateTime or sources.DateTimeOffset must be non-empty.", nameof(sources));
		}
		if(offset.HasValue && !hasOffsetSource)
		{
			throw new ArgumentException("When 'offset' is provided, one of sources.Offset or sources.DateTimeOffset must be non-empty.", nameof(sources));
		}

		return new TimestampCandidate(
			sourceType: sourceType,
			role: role,
			sources: sources,
			date: date,
			dateResolution: dateResolution,
			time: time,
			subSeconds: subSeconds,
			offset: offset
		);
	}


	public static TimestampCandidate FromRawDateWithTimeAndOffSetAndSubSec(
		TimestampSourceType sourceType,
		TimestampRole role,
		string rawDate,
		string? rawTime,
		string? rawOffset,
		string? rawSubSec,
		DateOnly date,
		ChronoDateResolution dateResolution,
		TimeOnly? time,
		TimeSpan? offset,
		long? subSec
	)
	{
		TimestampSources sources = new()
		{
			Date = rawDate,
			Time = rawTime,
			Offset = rawOffset,
			SubSeconds = rawSubSec
		};
		if(sources.IsEmpty)
		{
			throw new ArgumentException("At least one of rawDate, rawTime, rawOffset or rawSubSec must be non-empty.", nameof(rawDate));
		}
		if(time.HasValue && string.IsNullOrWhiteSpace(sources.Time))
		{
			throw new ArgumentException("When 'time' is provided, 'rawTime' must be non-empty.", nameof(rawTime));
		}
		if(offset.HasValue && string.IsNullOrWhiteSpace(sources.Offset))
		{
			throw new ArgumentException("When 'offset' is provided, 'rawOffset' must be non-empty.", nameof(rawOffset));
		}
		if(subSec.HasValue && string.IsNullOrWhiteSpace(sources.SubSeconds))
		{
			throw new ArgumentException("When 'subSec' is provided, 'rawSubSec' must be non-empty.", nameof(rawSubSec));
		}

		TimeOnly? timeWithoutFractions = time.HasValue
			? new TimeOnly(time.Value.Hour, time.Value.Minute, time.Value.Second)
			: null;

		long? nanosecondFractionsFromTime = TrimToNanoseconds(time);
		long? finalNanosecondsFractions = subSec ?? nanosecondFractionsFromTime;

		return new TimestampCandidate(
			sourceType: sourceType,
			role: role,
			sources: sources,
			date: date,
			dateResolution: dateResolution,
			time: timeWithoutFractions,
			subSeconds: finalNanosecondsFractions,
			offset: offset
		);
	}

	public static TimestampCandidate FromRawDateTimeWithOffsetAndSubSec(
		TimestampSourceType sourceType,
		TimestampRole role,
		string rawDateTime,
		string? rawOffset,
		string? rawSubSec,
		DateTime dt,
		TimeSpan? offset,
		long? subSec
	)
	{
		TimestampSources sources = new()
		{
			DateTime = rawDateTime,
			Offset = rawOffset,
			SubSeconds = rawSubSec
		};
		if(sources.IsEmpty)
		{
			throw new ArgumentException("At least one of rawDateTime, rawOffset or rawSubSec must be non-empty.", nameof(rawDateTime));
		}
		if(offset.HasValue && string.IsNullOrWhiteSpace(sources.Offset))
		{
			throw new ArgumentException("When 'offset' is provided, 'rawOffset' must be non-empty.", nameof(rawOffset));
		}
		if(subSec.HasValue && string.IsNullOrWhiteSpace(sources.SubSeconds))
		{
			throw new ArgumentException("When 'subSec' is provided, 'rawSubSec' must be non-empty.", nameof(rawSubSec));
		}

		DateOnly dateOnly = DateOnly.FromDateTime(dt);
		TimeOnly timeWithoutFractions = new TimeOnly(dt.Hour, dt.Minute, dt.Second);
		// Get sub-second fractions from the dt
		long? nanosecondFractionsFromDT = TrimToNanoseconds(dt);
		// If subSec is provided, it takes precedence over the value from dt
		long? finalNanosecondFractions = subSec ?? nanosecondFractionsFromDT;

		return new TimestampCandidate(
			sourceType: sourceType,
			role: role,
			sources: sources,
			date: dateOnly,
			dateResolution: ChronoDateResolution.FullDate,
			time: timeWithoutFractions,
			subSeconds: finalNanosecondFractions,
			offset: offset
		);
	}


	public static TimestampCandidate FromDateTimeOffsetWithSubSec(
		TimestampSourceType sourceType,
		TimestampRole role,
		string rawDateTimeOffset,
		string? rawSubSec,
		DateTimeOffset dateTimeOffset,
		long? subSec
	) {
		TimestampSources sources = new()
		{
			DateTimeOffset = rawDateTimeOffset,
			SubSeconds = rawSubSec
		};
		if(sources.IsEmpty)
		{
			throw new ArgumentException("At least one of rawDateTimeOffset or rawSubSec must be non-empty.", nameof(rawDateTimeOffset));
		}
		if(subSec.HasValue && string.IsNullOrWhiteSpace(sources.SubSeconds))
		{
			throw new ArgumentException("When 'subSec' is provided, 'rawSubSec' must be non-empty.", nameof(rawSubSec));
		}

		TimeSpan? offset = OffsetToTimeSpan(dateTimeOffset);
		
		long? nanosecondFractionsFromDTO = TrimToNanoseconds(dateTimeOffset);
		
		long? finalNanosecondFractions = subSec ?? nanosecondFractionsFromDTO;
		
		TimeOnly time = new(dateTimeOffset.Hour, dateTimeOffset.Minute, dateTimeOffset.Second);

		return new TimestampCandidate(
			sourceType: sourceType,
			role: role,
			sources: sources,
			date: DateOnly.FromDateTime(dateTimeOffset.DateTime),
			dateResolution: ChronoDateResolution.FullDate,
			time: time,
			subSeconds: finalNanosecondFractions,
			offset: offset
		);
	}
}