using BMTP3.Consoles.candidates.parsing;
using System;
using System.Collections.Generic;
using System.CommandLine.Parsing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Consoles.candidates;
public sealed record TimestampCandidate : IFormattable
{
	private static readonly ITimestampFormatter DefaultFormatter = new TimestampFormatter();

	public TimestampSourceType SourceType { get; init; }
	public TimestampRole Role { get; init; }
	public TimestampSources Source { get; init; }

	public DateOnly? Date { get; init; }
	public ChronoDateResolution? DateResolution { get; init; }
	public TimeOnly? Time { get; init; }

	/// <summary>
	/// 0 .. 999_999_999 nanoseconds of a second
	/// </summary>
	public long? SubSeconds { get; init; }

	public TimeSpan? Offset { get; init; }

	public TimestampCandidate(
		TimestampSourceType sourceType,
		TimestampRole role,
		TimestampSources sources,
		DateOnly? date,
		ChronoDateResolution? dateResolution,
		TimeOnly? time,
		long? subSeconds,
		TimeSpan? offset
	)
	{
		if(subSeconds is < 0 or > 999_999_999)
		{
			throw new ArgumentOutOfRangeException(
				nameof(subSeconds),
				subSeconds,
				"SubSeconds must be between 0 and 999,999,999 inclusive."
			);
		}

		SourceType = sourceType;
		Role = role;
		Source = sources;

		Date = date;
		if(date.HasValue && dateResolution is null)
		{
			throw new ArgumentNullException(nameof(dateResolution), "DateResolution must be provided when Date is specified.");
		}
		if(!date.HasValue && dateResolution.HasValue)
		{
			throw new ArgumentException("Date must be provided when DateResolution is specified.", nameof(date));
		}

		DateResolution = dateResolution;

		Time = time;
		SubSeconds = subSeconds;
		Offset = offset;
	}

	public bool IsEmpty => Date is null && Time is null && SubSeconds is null && Offset is null && Source.IsEmpty;


	/* ---------- Validation ---------- */

	/// <summary>
	/// Ensure the candidate fields form a valid compositon for formatting and conversion.
	/// Throws an <see cref="InvalidOperationException"/> when the composition is inconsistent,
	/// e.g. subseconds without a time component, or an offset without date/time.
	/// 
	/// This method is intended to be called before formatting or conversion operations.
	/// This method throws exception on invalid composition.
	/// </summary>
	public void EnsureValidComposition()
	{
		// SubSeconds only meaningful when a Time is present.
		if(SubSeconds.HasValue && Time is null)
		{
			throw new InvalidOperationException("TimestampCandidate has SubSeconds but no Time component.");
		}

		// An offset without either Date or Time is not a meaningful timestamp in this model.
		// (Offset is valid for time-only or date+time; but not alone.)
		if(Offset.HasValue && Time is null && Date is null)
		{
			throw new InvalidOperationException("TimestampCandidate has Offset but neither Date nor Time is present.");
		}

		// A time is only used for hour, minute, second; fractional seconds must be in SubSeconds.
		if(Time is not null && Time.Value.Ticks != 0)
		{
			throw new InvalidOperationException("TimeOnly must not contain fractional seconds. Use SubSeconds instead.");
		}
	}

	/// <summary>
	/// Determines whether the candidate fields form a valid composition.
	/// This method never throws exception.
	/// </summary>
	public bool IsValidComposition()
	{
		try
		{
			EnsureValidComposition();
			return true;
		} catch(InvalidOperationException)
		{
			return false;
		}
	}


	/* ---------- Conversion ---------- */

	public bool TryToDateTime(out DateTime resVal)
	{
		resVal = default;

		if(Date is null || Time is null)
		{
			return false;
		}

		long ticks = SubSeconds.HasValue
			? SubSeconds.Value / 100
			: 0;

		resVal = new DateTime(
			Date.Value.Year,
			Date.Value.Month,
			Date.Value.Day,
			Time.Value.Hour,
			Time.Value.Minute,
			Time.Value.Second,
			DateTimeKind.Unspecified
		).AddTicks(ticks);

		return true;
	}

	public bool TryToDateTimeOffset(out DateTimeOffset resVal)
	{
		resVal = default;

		if(!TryToDateTime(out DateTime dt))
		{
			return false;
		}

		if(Offset is null)
		{
			return false;
		}

		resVal = new DateTimeOffset(dt, Offset.Value);
		return true;
	}


	/* ---------- String output ---------- */

	/// <summary>
	/// Converts this timestamp to its string representation using a specified format string
	/// and an optional format provider.
	/// </summary>
	/// <param name="format">
	/// A format string that selects the <see cref="TimestampFormatStyle"/> to use.
	/// The value is parsed using <see cref="TimestampFormatStyleParser"/>.
	/// If <paramref name="format"/> is <c>null</c>, empty, or unrecognized,
	/// <see cref="TimestampFormatStyle.Iso8601_DotFraction"/> is used as the default.
	/// </param>
	/// <param name="formatProvider">
	/// An optional format provider.
	/// This parameter is ignored because timestamp formatting is culture-invariant
	/// and based on fixed, ISO-style representations.
	/// </param>
	/// <returns>
	/// A string representation of the timestamp formatted according to the resolved
	/// <see cref="TimestampFormatStyle"/>.
	/// </returns>
	/// <remarks>
	/// This method implements <see cref="IFormattable"/> to support composite formatting
	/// (e.g. <c>string.Format</c>, interpolated strings).
	///
	/// Formatting is deterministic and does not depend on culture settings.
	/// All separators, numeric formats, and symbols are fixed by the selected
	/// <see cref="TimestampFormatStyle"/>.
	///
	/// This method delegates to <see cref="ToString(TimestampFormatStyle)"/> for the
	/// actual formatting logic.
	/// </remarks>
	/// <remarks>
	/// The formatProvider parameter is ignored.
	/// Timestamp formatting is culture-invariant and ISO-based.
	/// </remarks>
	/// <exception cref="InvalidOperationException">
	/// Thrown if the timestamp has an invalid internal composition and cannot be formatted.
	/// </exception>
	public string ToString(string? format, IFormatProvider? formatProvider)
	{
		TimestampFormatStyle style =
			TimestampFormatStyleParser.ParseOrDefault(
				format,
				TimestampFormatStyle.Iso8601_DotFraction);

		return ToString(style);
	}

	/// <summary>
	/// Default string representation using ISO 8601 extended with dot-decimal fraction.
	/// Intended mainly for debugging and logging.
	/// </summary>
	public override string ToString()
	{
		return ToString(TimestampFormatStyle.Iso8601_DotFraction);
	}
	public string ToString(TimestampFormatStyle style)
	{
		return ToString(DefaultFormatter, style);
	}

	/// <summary>
	/// Returns a string representation of the timestamp in ISO 8601 format, including date, time, subseconds, and offset
	/// if available.
	/// 
	/// Most nice ISO 8601 formats:
	/// YYYY-MM-DDThh:mm:ss[,fff]Z
	/// YYYY-MM-DDThh:mm:ss[,fff]±hh[:mm[:ss[,fff]]]
	/// YYYY-MM-DDThh:mm:ss[,fff]±hH[mM[s[,fff]S]]
	/// 
	/// Most nice Hybrid RFC 3339 & ISO 8601 formats (not legal):
	/// YYYYMMDDThhmmss[,fff]Z
	/// YYYYMMDDThhmmss[,fff]±hh[mm[ss[,fff]]]
	/// 
	/// YYYY.MM.DD_hh.mm.ss[,fff]Z
	/// YYYY.MM.DD_hh.mm.ss[,fff]±hh[.mm[.ss[,fff]]]
	/// 
	/// Legal ISO 8601 formats produced:
	/// YYYY-MM-DDThh:mm:ss[.fff]Z
	/// YYYY-MM-DDThh:mm:ss[.fff]±hh[:mm[:ss[.fff]]]
	/// 
	/// YYYY-MM-DDThh:mm:ss[.fff]±hH[mM[:s[.fff]S]]
	/// 
	/// YYYY-MM-DDThh:mm:ss[,fff]Z
	/// YYYY-MM-DDThh:mm:ss[,fff]±hh[:mm[:ss[,fff]]]
	/// 
	/// YYYY-MM-DDThh:mm:ss[,fff]±hH[mM[s[,fff]S]]
	/// 
	/// Hybrid RFC 3339 & ISO 8601 format produced not legal:
	/// YYYYMMDDThhmmss[,fff]Z
	/// YYYYMMDDThhmmss[,fff]±hh[mm[ss[,fff]]]
	/// 
	/// YYYYMMDDThhmmss[.fff]Z
	/// YYYYMMDDThhmmss[.fff]±hh[mm[ss[.fff]]]
	/// 
	/// YYYYMMDD_hhmmss[,fff]Z
	/// YYYYMMDD_hhmmss[,fff]±hh[mm[ss[,fff]]]
	/// 
	/// YYYYMMDD_hhmmss[.fff]Z
	/// YYYYMMDD_hhmmss[.fff]±hh[mm[ss[.fff]]]
	/// 
	/// YYYY-MM-DDThh.mm.ss[,fff]Z
	/// YYYY-MM-DDThh.mm.ss[,fff]±hh[.mm[.ss[,fff]]]
	/// 
	/// YYYY.MM.DD_hh.mm.ss[,fff]Z
	/// YYYY.MM.DD_hh.mm.ss[,fff]±hh[.mm[.ss[,fff]]]
	/// 
	/// YYYY.MM.DDThh.mm.ss[,fff]Z
	/// YYYY.MM.DDThh.mm.ss[,fff]±hh[.mm[.ss[,fff]]]
	/// 
	/// </summary>
	/// <summary>
	/// Returns a string representation of the timestamp using the specified format style.
	/// Formatting rules (separators, fractional seconds, offset representation)
	/// are fully defined by <see cref="TimestampFormatStyle"/> and its descriptor.
	/// </summary>
	/// <param name="style">The timestamp output format style.</param>
	/// <returns>
	///		A formatted timestamp string that represents the timestamp, or "<empty timestamp>" if the candidate contains no date/time data.
	/// </returns>
	public string ToString(ITimestampFormatter formatter, TimestampFormatStyle style)
	{
		if(IsEmpty)
		{
			return "<empty timestamp>";
		}
		if(!IsValidComposition())
		{
			return "<illegal timestamp>";
		}

		return formatter.Format(this, style);
	}

	public string ToDebugString()
	{
		StringBuilder sb = new();

		sb.AppendLine("TimestampCandidate");
		sb.AppendLine($"  SourceType : {SourceType}");
		sb.AppendLine($"  Role       : {Role}");
		sb.AppendLine($"  Date       : {Date?.ToString() ?? "∅"}");
		sb.AppendLine($"  Time       : {Time?.ToString() ?? "∅"}");
		sb.AppendLine($"  SubSeconds : {SubSeconds?.ToString() ?? "∅"}");
		sb.AppendLine($"  Offset     : {Offset?.ToString() ?? "∅"}");

		if(!Source.IsEmpty)
		{
			sb.AppendLine("  Sources:");
			sb.Append(Source.ToDebugString());
		} else
		{
			sb.AppendLine("  Sources   : ∅");
		}

		return sb.ToString();
	}

	/* ---------- Factories ---------- */
}