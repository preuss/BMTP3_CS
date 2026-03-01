using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Consoles.candidates;

public static class TimestampFormatStyleParser
{
	public static TimestampFormatStyle ParseOrDefault(string? format, TimestampFormatStyle fallbackStyle)
	{
		switch(format)
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

		if(string.IsNullOrWhiteSpace(format))
		{
			return fallbackStyle;
		}

		if(Enum.TryParse(format, ignoreCase: true, out TimestampFormatStyle style))
		{
			return style;
		}

		return fallbackStyle;
	}
}

