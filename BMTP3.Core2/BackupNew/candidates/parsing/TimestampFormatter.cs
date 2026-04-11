using System.Text;

namespace BMTP3.Core2.BackupNew.candidates.parsing;

public class TimestampFormatter : ITimestampFormatter
{
	public string Format(TimestampCandidate candidate, TimestampFormatStyle formatStyle)
	{
		TimestampFormatDescriptor descriptor = TimestampFormatDescriptor.Describe(formatStyle);
		return Format(candidate, descriptor);
	}

	public string Format(TimestampCandidate candidate, TimestampFormatDescriptor descriptor)
	{
		candidate.EnsureValidComposition();

		List<string> outputParts = new();

		string? datePart = FormatDate(candidate.Date, descriptor.DateYearMonthSeparator,
			descriptor.DateMonthDaySeparator);
		string? timePart = FormatTime(candidate.Time, candidate.SubSeconds, descriptor.TimeFormat,
			descriptor.TimeClockSeparator, descriptor.TimeClockFormatStyle, descriptor.TimeFractionSeparator);
		string? offsetPart = FormatOffset(candidate.Offset, descriptor.OffsetFormat, descriptor.OffsetClockSeparator,
			descriptor.OffsetClockTrimStyle, descriptor.OffsetFractionSeparator);

		if (!string.IsNullOrWhiteSpace(datePart))
		{
			outputParts.Add(datePart);
		}

		if (!string.IsNullOrEmpty(datePart) && !string.IsNullOrWhiteSpace(timePart))
		{
			outputParts.Add(Separator(descriptor.DateTimeSeparatorStyle));
		}

		if (!string.IsNullOrEmpty(timePart))
		{
			outputParts.Add(timePart);
		}

		if (!string.IsNullOrEmpty(offsetPart))
		{
			outputParts.Add(offsetPart);
		}

		return string.Join(string.Empty, outputParts);
	}

	private string? FormatDate(DateOnly? dateOnly, DateComponentSeparator sepYearMonth,
		DateComponentSeparator sepMonthDay)
	{
		if (dateOnly is not DateOnly date)
		{
			return null;
		}

		string yearStr = date.Year.ToString("D4");
		string monthStr = date.Month.ToString("D2");
		string dayStr = date.Day.ToString("D2");

		return yearStr + Separator(sepYearMonth) + monthStr + Separator(sepMonthDay) + dayStr;
	}

	private string? FormatTime(TimeOnly? timeOnly, long? subSeconds, TimeRepresentation timeRepresentation,
		TimeClockSeparator? clockSep, TimeClockFormatStyle? clockTrimStyle, DecimalFractionSeparator fractionSeparator)
	{
		if (timeOnly is null && subSeconds.HasValue)
		{
			throw new ArgumentOutOfRangeException(nameof(subSeconds), subSeconds,
				"SubSeconds can not be present when Time is missing.");
		}

		if (timeOnly is not TimeOnly time)
		{
			return null;
		}

		if (timeRepresentation == TimeRepresentation.Clock)
		{
			if (clockSep == null)
			{
				throw new ArgumentOutOfRangeException(nameof(clockSep), clockSep,
					$"And {nameof(timeRepresentation)} of type {TimeRepresentation.Clock} needs a {nameof(clockSep)}");
			}

			if (clockTrimStyle == null)
			{
				throw new ArgumentOutOfRangeException(nameof(clockTrimStyle), clockTrimStyle,
					$"And {nameof(timeRepresentation)} of type {TimeRepresentation.Clock} needs a {nameof(clockTrimStyle)}");
			}

			return FormatTimeAsClock(time, subSeconds, clockSep.Value, clockTrimStyle.Value, fractionSeparator);
		}

		return FormatTimeAsDuration(time, subSeconds, fractionSeparator);
	}

	private string? FormatTimeAsClock(TimeOnly? timeOnly, long? subSeconds, TimeClockSeparator clockSep,
		TimeClockFormatStyle clockTrimStyle, DecimalFractionSeparator fractionSeparator)
	{
		if (timeOnly is null && subSeconds.HasValue)
		{
			throw new ArgumentOutOfRangeException(nameof(subSeconds), subSeconds,
				"SubSeconds can not be present when Time is missing.");
		}

		if (subSeconds < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(subSeconds), subSeconds,
				"SubSeconds must be null or minimum 0 and larger.");
		}

		if (timeOnly is not TimeOnly time)
		{
			return null;
		}

		int hours = time.Hour;
		int minutes = time.Minute;
		int seconds = time.Second;

		return FormatTimeValuesAsClock(hours, minutes, seconds, subSeconds, clockSep, clockTrimStyle,
			fractionSeparator);
	}

	private string FormatTimeValuesAsClock(int hours, int minutes, int seconds, long? fractions,
		TimeClockSeparator clockSep, TimeClockFormatStyle clockTrimStyle, DecimalFractionSeparator fractionSeparator)
	{
		// Negative time not allowed, ± sign handled outside this method
		if (hours < 0 || minutes < 0 || seconds < 0 || fractions < 0)
		{
			throw new ArgumentOutOfRangeException("Time can not be negative");
		}

		// No duration, return empty string, Z sign handled outside this method
		if (hours == 0 && minutes == 0 && seconds == 0 && (fractions ?? 0) == 0)
		{
			return string.Empty;
		}

		string? fractionStr = FormatFraction(fractions);

		bool includeFraction = !string.IsNullOrEmpty(fractionStr);
		bool includeSeconds =
			clockTrimStyle == TimeClockFormatStyle.Full
			|| seconds > 0
			|| includeFraction;

		StringBuilder timeStr = new();

		// Hours always included
		timeStr.Append(hours.ToString("D2"));
		// Minutes always included
		timeStr.Append(Separator(clockSep)).Append(minutes.ToString("D2"));

		// Seconds included only if > 0 or fraction present
		if (includeSeconds)
		{
			timeStr.Append(Separator(clockSep)).Append(seconds.ToString("D2"));

			if (includeFraction)
			{
				timeStr.Append(Separator(fractionSeparator)).Append(fractionStr);
			}
		}

		return timeStr.ToString();
	}

	private string? FormatFraction(long? subSeconds)
	{
		if (subSeconds < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(subSeconds), "SubSeconds must be minimum 0 and larger.");
		}

		if (subSeconds == null)
		{
			return null;
		}

		if (subSeconds == 0)
		{
			return string.Empty;
		}

		string fractionStr = subSeconds.Value.ToString("D9").TrimEnd('0');
		return fractionStr;
	}

	private string? FormatOffset(TimeSpan? offsetSpan, TimeRepresentation timeOffsetRepresentation,
		TimeClockSeparator? clockSep, TimeClockFormatStyle? clockTrimStyle, DecimalFractionSeparator fractionSeparator)
	{
		// If offset nonexisting or unknown, then empty offset.
		if (offsetSpan is not TimeSpan offset)
		{
			return string.Empty;
		}

		if (offset == TimeSpan.Zero)
		{
			return "Z";
		}

		string signStr = offset > TimeSpan.Zero ? "+" : "-";
		string absOffsetStr;
		if (timeOffsetRepresentation == TimeRepresentation.Clock)
		{
			if (clockSep == null)
			{
				throw new ArgumentOutOfRangeException(nameof(clockSep), clockSep,
					$"And {nameof(timeOffsetRepresentation)} of type {TimeRepresentation.Clock} needs a {nameof(clockSep)}");
			}

			if (clockTrimStyle == null)
			{
				throw new ArgumentOutOfRangeException(nameof(clockTrimStyle), clockTrimStyle,
					$"And {nameof(timeOffsetRepresentation)} of type {TimeRepresentation.Clock} needs a {nameof(clockTrimStyle)}");
			}

			absOffsetStr =
				FormatOffsetAsClock(offset.Duration(), clockSep.Value, clockTrimStyle.Value, fractionSeparator);
		}
		else
		{
			absOffsetStr = FormatOffsetAsDuration(offset.Duration(), fractionSeparator);
		}

		return signStr + absOffsetStr;
	}

	private string FormatOffsetAsClock(TimeSpan offset, TimeClockSeparator clockSep,
		TimeClockFormatStyle clockTrimStyle, DecimalFractionSeparator fractionSeparator)
	{
		if (offset < TimeSpan.Zero)
		{
			throw new ArgumentOutOfRangeException(nameof(offset), offset,
				"Offset must be non-negative (absolute) when formatting clock offset.");
		}

		if (offset == TimeSpan.Zero)
		{
			return string.Empty;
		}

		TimeSpan absOffset = offset;

		int totalHours = absOffset.Hours + absOffset.Days * 24;
		int minutes = absOffset.Minutes;
		int seconds = absOffset.Seconds;

		long subSeconds = absOffset.Ticks % TimeSpan.TicksPerSecond * TimeSpan.NanosecondsPerTick;

		// FormatTimeValuesAsClock already appends the fraction when present.
		string formattedTimeStr = FormatTimeValuesAsClock(totalHours, minutes, seconds, subSeconds, clockSep,
			clockTrimStyle, fractionSeparator);
		return formattedTimeStr;
	}

	private string FormatOffsetAsDuration(TimeSpan offset, DecimalFractionSeparator fractionSep)
	{
		if (offset < TimeSpan.Zero)
		{
			throw new ArgumentOutOfRangeException(nameof(offset), offset,
				"Offset must be non-negative (absolute) when formatting duration offset.");
		}

		if (offset == TimeSpan.Zero)
		{
			return string.Empty;
		}

		TimeSpan absOffset = offset;

		int totalHours = absOffset.Hours + absOffset.Days * 24;
		int minutes = absOffset.Minutes;
		int seconds = absOffset.Seconds;

		long subSeconds = absOffset.Ticks % TimeSpan.TicksPerSecond * TimeSpan.NanosecondsPerTick;

		string formattetDurationOffset =
			FormatTimeValuesAsDuration(totalHours, minutes, seconds, subSeconds, fractionSep);
		return formattetDurationOffset;
	}

	private string FormatTimeAsDuration(TimeOnly time, long? fractions, DecimalFractionSeparator sep)
	{
		int hours = time.Hour;
		int minutes = time.Minute;
		int seconds = time.Second;
		return FormatTimeValuesAsDuration(hours, minutes, seconds, fractions, sep);
	}

	private string FormatTimeValuesAsDuration(int hours, int minutes, int seconds, long? fractions,
		DecimalFractionSeparator sep)
	{
		// Negative time not allowed, ± sign handled outside this method
		if (hours < 0 || minutes < 0 || seconds < 0 || fractions < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(hours), hours, "Time can not be negative");
		}

		// No duration, return empty string, Z sign handled outside this method
		if (hours == 0 && minutes == 0 && seconds == 0 && (fractions ?? 0) == 0)
		{
			return string.Empty;
		}

		string? fractionStr = FormatFraction(fractions);

		bool includeFraction = !string.IsNullOrEmpty(fractionStr);
		bool includeSeconds = seconds > 0 || includeFraction;
		bool includeMinutes = minutes > 0 || includeSeconds;

		StringBuilder durationStr = new();

		// Hours always included
		durationStr.Append(hours.ToString()).Append("H");

		if (includeMinutes)
		{
			durationStr.Append(minutes.ToString()).Append("M");
		}

		if (includeSeconds)
		{
			durationStr.Append(seconds.ToString());
			if (includeFraction)
			{
				durationStr.Append(Separator(sep)).Append(fractionStr);
			}

			durationStr.Append("S");
		}

		return durationStr.ToString();
	}

	private string Separator(DecimalFractionSeparator sep)
	{
		return sep switch
		{
			DecimalFractionSeparator.Dot => ".",
			DecimalFractionSeparator.Comma => ",",
			_ => throw new ArgumentOutOfRangeException(nameof(sep), sep, "Unknown separator.")
		};
	}

	private string Separator(TimeClockSeparator sep)
	{
		return sep switch
		{
			TimeClockSeparator.Colon => ":",
			TimeClockSeparator.Dot => ".",
			TimeClockSeparator.None => string.Empty,
			_ => throw new ArgumentOutOfRangeException(nameof(sep), sep, "Unknown separator.")
		};
	}

	private string Separator(DateTimeSeparatorStyle sep)
	{
		return sep switch
		{
			DateTimeSeparatorStyle.T => "T",
			DateTimeSeparatorStyle.Underscore => "_",
			_ => throw new ArgumentOutOfRangeException(nameof(sep), sep, "Unknown separator.")
		};
	}

	private string Separator(DateComponentSeparator sep)
	{
		return sep switch
		{
			DateComponentSeparator.None => string.Empty,
			DateComponentSeparator
				.Hyphen_Minus => "\u002D", // YYYY-MM-DD, U+002D, 0x2D, standard ASCII hyphen-minus, ISO 8601 default
			DateComponentSeparator.Hyphen => "\u2010", // YYYY‐MM‐DD, U+2010, 0x2010, true hyphen
			DateComponentSeparator
				.Minus_Sign => "\u2212", // YYYY−MM−DD, U+2212, 0x2212, mathematical minus sign, ISO 8601 recommended
			DateComponentSeparator.Heavy_Minus_Sign => "\u2796", // YYYY➖MM➖DD, U+2796, 0x2796, heavier minus sign
			DateComponentSeparator.Figure_Dash => "\u2012", // YYYY‒MM‒DD, U+2012, 0x2012, width of digits
			DateComponentSeparator.EnDash => "\u2013", // YYYY–MM–DD, U+2013, 0x2013, slightly longer than Figure Dash
			DateComponentSeparator.EmDash => "\u2014", // YYYY—MM—DD, U+2014, 0x2014, longest dash
			DateComponentSeparator.Dot => "\u002E", // YYYY.MM.DD, U+002E, 0x2E, standard ASCII dot
			DateComponentSeparator.Slash => "\u002F", // YYYY/MM/DD, U+002F, 0x2F, standard ASCII slash
			DateComponentSeparator.Space => "\u0020", // YYYY MM DD, U+0020, 0x20, standard ASCII space
			_ => throw new ArgumentOutOfRangeException(nameof(sep), sep, "Unknown separator.")
		};
	}
}