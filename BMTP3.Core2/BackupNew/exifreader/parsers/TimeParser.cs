using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.exifreader.parsers;

public class TimeParser : ParserBase<TimeOnly>
{
	public static readonly string[] TimeFormats =
	{
		"HH:mm:ss.FFFFFFF",
		"HH:mm:ss,FFFFFFF",
		"HH:mm:ss",
		"HH:mm",
	};

	public override bool TryParse(string? raw, out TimeOnly result)
	{
		result = default;

		if(string.IsNullOrWhiteSpace(raw))
		{
			return false;
		}
		raw = raw.Trim();

		return TimeOnly.TryParseExact(
			raw,
			TimeFormats,
			CultureInfo.InvariantCulture,
			DateTimeStyles.None,
			out result
		);
	}
}
