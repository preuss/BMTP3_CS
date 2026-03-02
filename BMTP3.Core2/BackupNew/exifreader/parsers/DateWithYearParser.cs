using BMTP3.Core2.BackupNew.candidates;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.exifreader.parsers;
public sealed class DateWithYearParser : DateParserBase
{
	private static readonly string[] YearFormats = ["yyyy"];
	public DateWithYearParser() : 
		base(
			ChronoDateResolution.YearOnly, 
			YearFormats
		) { }
}