using BMTP3.Core2.BackupNew.candidates;

namespace BMTP3.Core2.BackupNew.exifreader.parsers;

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