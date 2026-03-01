using BMTP3.Consoles.candidates;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Consoles.exifreader.parsers;
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