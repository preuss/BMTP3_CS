using System.Globalization;

namespace BMTP3.Core4.Engine.TimeStamp.Parsers;

public class TimeParser : ParserBase<TimeOnly>
{
	public static readonly string[] TimeFormats =
	{
		"HH:mm:ss.FFFFFFF",
		"HH:mm:ss,FFFFFFF",
		"HH:mm:ss",
		"HH:mm"
	};

	public override bool TryParse(string? raw, out TimeOnly result)
	{
		result = default;

		if (string.IsNullOrWhiteSpace(raw))
		{
			return false;
		}

		raw = raw.Trim();

		return TimeOnly.TryParseExact(
			raw,
			TimeFormats,
			CultureInfo.InvariantCulture,
			DateTimeStyles.None,
			out result
		);
	}
}