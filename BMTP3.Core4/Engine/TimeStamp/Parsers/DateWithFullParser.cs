using BMTP3.Core4.Engine.TimeStamp.Candidates;

namespace BMTP3.Core4.Engine.TimeStamp.Parsers;

public sealed class DateWithFullParser : DateParserBase
{
	private static readonly string[] FullDateFormats =
	{
		"yyyy:MM:dd",
		"yyyy-MM-dd",
		"yyyy.MM.dd",
		"yyyy/MM/dd",
		"yyyy MM dd",
		"yyyyMMdd"
	};

	public DateWithFullParser() : base(ChronoDateResolution.FullDate, FullDateFormats)
	{
	}
}