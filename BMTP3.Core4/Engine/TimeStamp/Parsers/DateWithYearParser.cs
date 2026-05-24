using BMTP3.Core4.Engine.TimeStamp.Candidates;

namespace BMTP3.Core4.Engine.TimeStamp.Parsers;

public sealed class DateWithYearParser : DateParserBase
{
	private static readonly string[] YearFormats = ["yyyy"];

	public DateWithYearParser() :
		base(
			ChronoDateResolution.YearOnly,
			YearFormats
		)
	{
	}
}