using BMTP3.Core2.BackupNew.candidates;

namespace BMTP3.Core2.BackupNew.exifreader.parsers;

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