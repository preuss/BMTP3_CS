using BMTP3.Consoles.candidates;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BMTP3.Consoles.exifreader.parsers;

/// <summary>
/// Always use 
/// first DateFullParser, 
/// then DateYearMonthParser, 
/// then DateYearParser 
/// to be sure that you have all possible resolutions covered.
/// </summary>
public abstract class DateParserBase : ParserBase<DateOnly>
{
	public ChronoDateResolution Resolution { get; init; }
	public string[] Formats { get; init; }

	protected DateParserBase(ChronoDateResolution resolution, string[] formats)
	{
		Resolution = resolution;
		Formats = formats;
	}
	public override bool TryParse(string? raw, out DateOnly result)
	{
		result = default;
		if(string.IsNullOrWhiteSpace(raw)) return false;
		raw = raw.Trim();

		return DateOnly.TryParseExact(
			raw,
			Formats,
			CultureInfo.InvariantCulture,
			DateTimeStyles.None,
			out result
		);
	}
}
