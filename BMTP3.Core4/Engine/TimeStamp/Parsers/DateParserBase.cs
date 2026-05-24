using System.Globalization;
using BMTP3.Core4.Engine.TimeStamp.Candidates;

namespace BMTP3.Core4.Engine.TimeStamp.Parsers;

/// <summary>
///     Always use
///     first DateFullParser,
///     then DateYearMonthParser,
///     then DateYearParser
///     to be sure that you have all possible resolutions covered.
/// </summary>
public abstract class DateParserBase : ParserBase<DateOnly>
{
	protected DateParserBase(ChronoDateResolution resolution, string[] formats)
	{
		Resolution = resolution;
		Formats = formats;
	}

	public ChronoDateResolution Resolution { get; init; }
	public string[] Formats { get; init; }

	public override bool TryParse(string? raw, out DateOnly result)
	{
		result = default;
		if (string.IsNullOrWhiteSpace(raw))
		{
			return false;
		}

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