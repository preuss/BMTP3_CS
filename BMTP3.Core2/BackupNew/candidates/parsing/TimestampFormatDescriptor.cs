namespace BMTP3.Core2.BackupNew.candidates.parsing;

public sealed class TimestampFormatDescriptor
{
	public DateComponentSeparator DateYearMonthSeparator { get; init; }
	public DateComponentSeparator DateMonthDaySeparator { get; init; }
	public TimeRepresentation TimeFormat { get; init; }
	public TimeClockSeparator TimeClockSeparator { get; init; }
	public TimeClockFormatStyle TimeClockFormatStyle { get; init; }
	public DecimalFractionSeparator TimeFractionSeparator { get; init; }
	public DateTimeSeparatorStyle DateTimeSeparatorStyle { get; init; }
	public TimeRepresentation OffsetFormat { get; init; }
	public TimeClockSeparator OffsetClockSeparator { get; init; }
	public TimeClockFormatStyle OffsetClockTrimStyle { get; init; }
	public DecimalFractionSeparator OffsetFractionSeparator { get; init; }

	public static TimestampFormatDescriptor Describe(TimestampFormatStyle style)
	{
		// All descriptors populated explicitly so callers don't need to handle nulls/defaults.
		switch (style)
		{
			// ── Standard ISO 8601 Extended (with colon)
			case TimestampFormatStyle.Iso8601_DotFraction:
				return new TimestampFormatDescriptor
				{
					DateYearMonthSeparator = DateComponentSeparator.Hyphen_Minus,
					DateMonthDaySeparator = DateComponentSeparator.Hyphen_Minus,
					TimeFormat = TimeRepresentation.Clock,
					TimeClockSeparator = TimeClockSeparator.Colon,
					TimeClockFormatStyle = TimeClockFormatStyle.Full,
					TimeFractionSeparator = DecimalFractionSeparator.Dot,
					DateTimeSeparatorStyle = DateTimeSeparatorStyle.T,
					OffsetFormat = TimeRepresentation.Clock,
					OffsetClockSeparator = TimeClockSeparator.Colon,
					OffsetClockTrimStyle = TimeClockFormatStyle.Full,
					OffsetFractionSeparator = DecimalFractionSeparator.Dot
				};

			case TimestampFormatStyle.Iso8601_DotFraction_Compact:
				return new TimestampFormatDescriptor
				{
					DateYearMonthSeparator = DateComponentSeparator.None,
					DateMonthDaySeparator = DateComponentSeparator.None,
					TimeFormat = TimeRepresentation.Clock,
					TimeClockSeparator = TimeClockSeparator.None,
					TimeClockFormatStyle = TimeClockFormatStyle.Full,
					TimeFractionSeparator = DecimalFractionSeparator.Dot,
					DateTimeSeparatorStyle = DateTimeSeparatorStyle.T,
					OffsetFormat = TimeRepresentation.Clock,
					OffsetClockSeparator = TimeClockSeparator.None,
					OffsetClockTrimStyle = TimeClockFormatStyle.Full,
					OffsetFractionSeparator = DecimalFractionSeparator.Dot
				};

			case TimestampFormatStyle.Iso8601_CommaFraction:
				return new TimestampFormatDescriptor
				{
					DateYearMonthSeparator = DateComponentSeparator.Hyphen_Minus,
					DateMonthDaySeparator = DateComponentSeparator.Hyphen_Minus,
					TimeFormat = TimeRepresentation.Clock,
					TimeClockSeparator = TimeClockSeparator.Colon,
					TimeClockFormatStyle = TimeClockFormatStyle.Full,
					TimeFractionSeparator = DecimalFractionSeparator.Comma,
					DateTimeSeparatorStyle = DateTimeSeparatorStyle.T,
					OffsetFormat = TimeRepresentation.Clock,
					OffsetClockSeparator = TimeClockSeparator.Colon,
					OffsetClockTrimStyle = TimeClockFormatStyle.Full,
					OffsetFractionSeparator = DecimalFractionSeparator.Comma
				};

			case TimestampFormatStyle.Iso8601_DotFraction_OffsetDuration:
				return new TimestampFormatDescriptor
				{
					DateYearMonthSeparator = DateComponentSeparator.Hyphen_Minus,
					DateMonthDaySeparator = DateComponentSeparator.Hyphen_Minus,
					TimeFormat = TimeRepresentation.Clock,
					TimeClockSeparator = TimeClockSeparator.Colon,
					TimeClockFormatStyle = TimeClockFormatStyle.Full,
					TimeFractionSeparator = DecimalFractionSeparator.Dot,
					DateTimeSeparatorStyle = DateTimeSeparatorStyle.T,
					OffsetFormat = TimeRepresentation.Duration,
					OffsetClockSeparator = TimeClockSeparator.None,
					OffsetClockTrimStyle = TimeClockFormatStyle.Full,
					OffsetFractionSeparator = DecimalFractionSeparator.Dot
				};

			case TimestampFormatStyle.Iso8601_CommaFraction_OffsetDuration:
				return new TimestampFormatDescriptor
				{
					DateYearMonthSeparator = DateComponentSeparator.Hyphen_Minus,
					DateMonthDaySeparator = DateComponentSeparator.Hyphen_Minus,
					TimeFormat = TimeRepresentation.Clock,
					TimeClockSeparator = TimeClockSeparator.Colon,
					TimeClockFormatStyle = TimeClockFormatStyle.Full,
					TimeFractionSeparator = DecimalFractionSeparator.Comma,
					DateTimeSeparatorStyle = DateTimeSeparatorStyle.T,
					OffsetFormat = TimeRepresentation.Duration,
					OffsetClockSeparator = TimeClockSeparator.None,
					OffsetClockTrimStyle = TimeClockFormatStyle.Full,
					OffsetFractionSeparator = DecimalFractionSeparator.Comma
				};

			// ── Compact basic formats
			case TimestampFormatStyle.Compact_DotFraction:
				return new TimestampFormatDescriptor
				{
					DateYearMonthSeparator = DateComponentSeparator.None,
					DateMonthDaySeparator = DateComponentSeparator.None,
					TimeFormat = TimeRepresentation.Clock,
					TimeClockSeparator = TimeClockSeparator.None,
					TimeClockFormatStyle = TimeClockFormatStyle.Full,
					TimeFractionSeparator = DecimalFractionSeparator.Dot,
					DateTimeSeparatorStyle = DateTimeSeparatorStyle.T,
					OffsetFormat = TimeRepresentation.Clock,
					OffsetClockSeparator = TimeClockSeparator.None,
					OffsetClockTrimStyle = TimeClockFormatStyle.Full,
					OffsetFractionSeparator = DecimalFractionSeparator.Dot
				};

			case TimestampFormatStyle.Compact_CommaFraction:
				return new TimestampFormatDescriptor
				{
					DateYearMonthSeparator = DateComponentSeparator.None,
					DateMonthDaySeparator = DateComponentSeparator.None,
					TimeFormat = TimeRepresentation.Clock,
					TimeClockSeparator = TimeClockSeparator.None,
					TimeClockFormatStyle = TimeClockFormatStyle.Full,
					TimeFractionSeparator = DecimalFractionSeparator.Comma,
					DateTimeSeparatorStyle = DateTimeSeparatorStyle.T,
					OffsetFormat = TimeRepresentation.Clock,
					OffsetClockSeparator = TimeClockSeparator.None,
					OffsetClockTrimStyle = TimeClockFormatStyle.Full,
					OffsetFractionSeparator = DecimalFractionSeparator.Comma
				};

			case TimestampFormatStyle.Compact_Underscore_DotFraction:
				return new TimestampFormatDescriptor
				{
					DateYearMonthSeparator = DateComponentSeparator.None,
					DateMonthDaySeparator = DateComponentSeparator.None,
					TimeFormat = TimeRepresentation.Clock,
					TimeClockSeparator = TimeClockSeparator.None,
					TimeClockFormatStyle = TimeClockFormatStyle.Full,
					TimeFractionSeparator = DecimalFractionSeparator.Dot,
					DateTimeSeparatorStyle = DateTimeSeparatorStyle.Underscore,
					OffsetFormat = TimeRepresentation.Clock,
					OffsetClockSeparator = TimeClockSeparator.None,
					OffsetClockTrimStyle = TimeClockFormatStyle.Full,
					OffsetFractionSeparator = DecimalFractionSeparator.Dot
				};

			case TimestampFormatStyle.Compact_Underscore_CommaFraction:
				return new TimestampFormatDescriptor
				{
					DateYearMonthSeparator = DateComponentSeparator.None,
					DateMonthDaySeparator = DateComponentSeparator.None,
					TimeFormat = TimeRepresentation.Clock,
					TimeClockSeparator = TimeClockSeparator.None,
					TimeClockFormatStyle = TimeClockFormatStyle.Full,
					TimeFractionSeparator = DecimalFractionSeparator.Comma,
					DateTimeSeparatorStyle = DateTimeSeparatorStyle.Underscore,
					OffsetFormat = TimeRepresentation.Clock,
					OffsetClockSeparator = TimeClockSeparator.None,
					OffsetClockTrimStyle = TimeClockFormatStyle.Full,
					OffsetFractionSeparator = DecimalFractionSeparator.Comma
				};

			// ── Dot-time variants
			case TimestampFormatStyle.DotTime_DotFraction:
				return new TimestampFormatDescriptor
				{
					DateYearMonthSeparator = DateComponentSeparator.Hyphen_Minus,
					DateMonthDaySeparator = DateComponentSeparator.Hyphen_Minus,
					TimeFormat = TimeRepresentation.Clock,
					TimeClockSeparator = TimeClockSeparator.Dot,
					TimeClockFormatStyle = TimeClockFormatStyle.Full,
					TimeFractionSeparator = DecimalFractionSeparator.Dot,
					DateTimeSeparatorStyle = DateTimeSeparatorStyle.T,
					OffsetFormat = TimeRepresentation.Clock,
					OffsetClockSeparator = TimeClockSeparator.Dot,
					OffsetClockTrimStyle = TimeClockFormatStyle.Full,
					OffsetFractionSeparator = DecimalFractionSeparator.Dot
				};

			case TimestampFormatStyle.DotTime_CommaFraction:
				return new TimestampFormatDescriptor
				{
					DateYearMonthSeparator = DateComponentSeparator.Hyphen_Minus,
					DateMonthDaySeparator = DateComponentSeparator.Hyphen_Minus,
					TimeFormat = TimeRepresentation.Clock,
					TimeClockSeparator = TimeClockSeparator.Dot,
					TimeClockFormatStyle = TimeClockFormatStyle.Full,
					TimeFractionSeparator = DecimalFractionSeparator.Comma,
					DateTimeSeparatorStyle = DateTimeSeparatorStyle.T,
					OffsetFormat = TimeRepresentation.Clock,
					OffsetClockSeparator = TimeClockSeparator.Dot,
					OffsetClockTrimStyle = TimeClockFormatStyle.Full,
					OffsetFractionSeparator = DecimalFractionSeparator.Comma
				};

			// ── Dot-date variants
			case TimestampFormatStyle.DotDateTime_Underscore_DotFraction:
				return new TimestampFormatDescriptor
				{
					DateYearMonthSeparator = DateComponentSeparator.Dot,
					DateMonthDaySeparator = DateComponentSeparator.Dot,
					TimeFormat = TimeRepresentation.Clock,
					TimeClockSeparator = TimeClockSeparator.Dot,
					TimeClockFormatStyle = TimeClockFormatStyle.Full,
					TimeFractionSeparator = DecimalFractionSeparator.Dot,
					DateTimeSeparatorStyle = DateTimeSeparatorStyle.Underscore,
					OffsetFormat = TimeRepresentation.Clock,
					OffsetClockSeparator = TimeClockSeparator.Dot,
					OffsetClockTrimStyle = TimeClockFormatStyle.Full,
					OffsetFractionSeparator = DecimalFractionSeparator.Dot
				};

			case TimestampFormatStyle.DotDateTime_Underscore_CommaFraction:
				return new TimestampFormatDescriptor
				{
					DateYearMonthSeparator = DateComponentSeparator.Dot,
					DateMonthDaySeparator = DateComponentSeparator.Dot,
					TimeFormat = TimeRepresentation.Clock,
					TimeClockSeparator = TimeClockSeparator.Dot,
					TimeClockFormatStyle = TimeClockFormatStyle.Full,
					TimeFractionSeparator = DecimalFractionSeparator.Comma,
					DateTimeSeparatorStyle = DateTimeSeparatorStyle.Underscore,
					OffsetFormat = TimeRepresentation.Clock,
					OffsetClockSeparator = TimeClockSeparator.Dot,
					OffsetClockTrimStyle = TimeClockFormatStyle.Full,
					OffsetFractionSeparator = DecimalFractionSeparator.Comma
				};

			case TimestampFormatStyle.DotDateTime_DotFraction:
				return new TimestampFormatDescriptor
				{
					DateYearMonthSeparator = DateComponentSeparator.Dot,
					DateMonthDaySeparator = DateComponentSeparator.Dot,
					TimeFormat = TimeRepresentation.Clock,
					TimeClockSeparator = TimeClockSeparator.Dot,
					TimeClockFormatStyle = TimeClockFormatStyle.Full,
					TimeFractionSeparator = DecimalFractionSeparator.Dot,
					DateTimeSeparatorStyle = DateTimeSeparatorStyle.T,
					OffsetFormat = TimeRepresentation.Clock,
					OffsetClockSeparator = TimeClockSeparator.Dot,
					OffsetClockTrimStyle = TimeClockFormatStyle.Full,
					OffsetFractionSeparator = DecimalFractionSeparator.Dot
				};

			case TimestampFormatStyle.DotDateTime_CommaFraction:
				return new TimestampFormatDescriptor
				{
					DateYearMonthSeparator = DateComponentSeparator.Dot,
					DateMonthDaySeparator = DateComponentSeparator.Dot,
					TimeFormat = TimeRepresentation.Clock,
					TimeClockSeparator = TimeClockSeparator.Dot,
					TimeClockFormatStyle = TimeClockFormatStyle.Full,
					TimeFractionSeparator = DecimalFractionSeparator.Comma,
					DateTimeSeparatorStyle = DateTimeSeparatorStyle.T,
					OffsetFormat = TimeRepresentation.Clock,
					OffsetClockSeparator = TimeClockSeparator.Dot,
					OffsetClockTrimStyle = TimeClockFormatStyle.Full,
					OffsetFractionSeparator = DecimalFractionSeparator.Comma
				};

			// ── Danish separator variants
			case TimestampFormatStyle.DanishSeparator_CommaFraction:
				return new TimestampFormatDescriptor
				{
					DateYearMonthSeparator = DateComponentSeparator.Hyphen_Minus,
					DateMonthDaySeparator = DateComponentSeparator.Slash,
					TimeFormat = TimeRepresentation.Clock,
					TimeClockSeparator = TimeClockSeparator.Colon,
					TimeClockFormatStyle = TimeClockFormatStyle.Full,
					TimeFractionSeparator = DecimalFractionSeparator.Comma,
					DateTimeSeparatorStyle = DateTimeSeparatorStyle.T,
					OffsetFormat = TimeRepresentation.Clock,
					OffsetClockSeparator = TimeClockSeparator.Colon,
					OffsetClockTrimStyle = TimeClockFormatStyle.Full,
					OffsetFractionSeparator = DecimalFractionSeparator.Comma
				};

			case TimestampFormatStyle.DanishSeparator_Underscore_CommaFraction:
				return new TimestampFormatDescriptor
				{
					DateYearMonthSeparator = DateComponentSeparator.Hyphen_Minus,
					DateMonthDaySeparator = DateComponentSeparator.Slash,
					TimeFormat = TimeRepresentation.Clock,
					TimeClockSeparator = TimeClockSeparator.Colon,
					TimeClockFormatStyle = TimeClockFormatStyle.Full,
					TimeFractionSeparator = DecimalFractionSeparator.Comma,
					DateTimeSeparatorStyle = DateTimeSeparatorStyle.Underscore,
					OffsetFormat = TimeRepresentation.Clock,
					OffsetClockSeparator = TimeClockSeparator.Colon,
					OffsetClockTrimStyle = TimeClockFormatStyle.Full,
					OffsetFractionSeparator = DecimalFractionSeparator.Comma
				};

			default:
				throw new ArgumentOutOfRangeException(nameof(style), style, "Unhandled TimestampFormatStyle");
		}
	}
}