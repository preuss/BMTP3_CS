using BMTP3.Core2.BackupNew.candidates;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.exifreader.parsers;
public sealed class DateWithYearMonthParser : DateParserBase
{
	private static readonly string[] YearMonthFormats = {
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
	{ }
}