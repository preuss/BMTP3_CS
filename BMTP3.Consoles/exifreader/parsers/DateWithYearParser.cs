using BMTP3.Consoles.candidates;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Consoles.exifreader.parsers;
public sealed class DateWithYearParser : DateParserBase
{
	private static readonly string[] YearFormats = ["yyyy"];
	public DateWithYearParser() : 
		base(
			ChronoDateResolution.YearOnly, 
			YearFormats
		) { }
}