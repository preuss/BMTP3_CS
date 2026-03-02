using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.exifreader.parsers;

public class OffsetParser : ParserBase<TimeSpan>
{
	private static readonly string[] OffsetFormats =
	{
		"hh':'mm':'ss'.'FFFFFFF",	// ±HH:mm:ss.FFFFFFF
		"hh':'mm':'ss','FFFFFFF",	// ±HH:mm:ss,FFFFFFF
		"hh':'mm':'ss",				// ±HH:mm:ss
        "hh':'mm",					// ±HH:mm
		"hhmmss'.'FFFFFFF",			// ±HHMMSS.FFFFFFF
		"hhmmss','FFFFFFF",			// ±HHMMSS,FFFFFFF
		"hhmmss",					// ±HHMMSS
		"hh",						// ±HH
    };
	public override bool TryParse(string? raw, [NotNullWhen(true)] out TimeSpan result)
	{
		result = default;

		if(string.IsNullOrWhiteSpace(raw))
		{
			return false;
		}

		raw = raw.Trim();

		// Zulu time, UTC time
		if(raw == "Z")
		{
			result = TimeSpan.Zero;
			return true;
		}

		bool parsed = TimeSpan.TryParseExact(
			raw, 
			OffsetFormats,
			CultureInfo.InvariantCulture,
			TimeSpanStyles.None,
			out result
		);
		if(parsed) return true;

		string[] PlusOffsetFormats = OffsetFormats
			.Select(format => "'+'" + format)
			.ToArray();
		parsed = TimeSpan.TryParseExact(
			raw,
			PlusOffsetFormats,
			CultureInfo.InvariantCulture,
			TimeSpanStyles.None,
			out result
		);
		if(parsed) return true;
		string[] MinusOffsetFormats = OffsetFormats
			.Select(format => "'-'" + format)
			.ToArray();
		parsed = TimeSpan.TryParseExact(
			raw,
			MinusOffsetFormats,
			CultureInfo.InvariantCulture,
			TimeSpanStyles.None,
			out result
		);
		if(parsed) { 
			result = -result;
			return true;
		}
		return false;
	}
}
