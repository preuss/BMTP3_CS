using BMTP3.Core2.BackupNew.candidates;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.exifreader.parsers;

public sealed class DateWithFullParser : DateParserBase
{
	private static readonly string[] FullDateFormats = {
		"yyyy:MM:dd",
		"yyyy-MM-dd",
		"yyyy.MM.dd",
		"yyyy/MM/dd",
		"yyyy MM dd",
		"yyyyMMdd"
	};
	public DateWithFullParser() : base(ChronoDateResolution.FullDate, FullDateFormats) { }
}