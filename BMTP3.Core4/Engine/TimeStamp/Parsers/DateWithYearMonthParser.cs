using BMTP3.Core4.Engine.TimeStamp.Candidates;

namespace BMTP3.Core4.Engine.TimeStamp.Parsers;

public sealed class DateWithYearMonthParser : DateParserBase
{
	private static readonly string[] YearMonthFormats =
	{
		"yyyy:MM",
		"yyyy-MM",
		"yyyy.MM",
		"yyyy/MM",
		"yyyy MM",
		"yyyyMM"
	};

	public DateWithYearMonthParser() :
		base(
			ChronoDateResolution.YearAndMonth,
			YearMonthFormats
		)
	{
	}
}