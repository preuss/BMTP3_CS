namespace BMTP3.Core2.BackupNew.candidates;

public static class TimestampFormatStyleParser
{
	public static TimestampFormatStyle ParseOrDefault(string? format, TimestampFormatStyle fallbackStyle)
	{
		switch (format)
		{
			case null:
			case "":
			case "o":
			case "O":
			case "iso":
				return TimestampFormatStyle.Iso8601_DotFraction;

			case "r":
			case "R":
				return TimestampFormatStyle.Iso8601_CommaFraction;
			case "compact":
				return TimestampFormatStyle.Compact_DotFraction;
		}

		if (string.IsNullOrWhiteSpace(format))
		{
			return fallbackStyle;
		}

		if (Enum.TryParse(format, true, out TimestampFormatStyle style))
		{
			return style;
		}

		return fallbackStyle;
	}
}