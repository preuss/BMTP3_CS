using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Consoles.exifreader.parsers;

public sealed class DateTimeParser : ParserBase<DateTime>
{
	private static readonly string[] DateTimeFormats =
	{
		"yyyy:MM:dd HH:mm:ss.FFFFFFF",
		"yyyy-MM-dd HH:mm:ss.FFFFFFF",
		"yyyy.MM.dd HH:mm:ss.FFFFFFF",
		"yyyy/MM/dd HH:mm:ss.FFFFFFF",
		"yyyy MM dd HH:mm:ss.FFFFFFF",
		"yyyy:MM:ddTHH:mm:ss.FFFFFFF",
		"yyyy-MM-ddTHH:mm:ss.FFFFFFF",
		"yyyy.MM.ddTHH:mm:ss.FFFFFFF",
		"yyyy/MM/ddTHH:mm:ss.FFFFFFF",
		"yyyy MM ddTHH:mm:ss.FFFFFFF",

		"yyyy:MM:dd HH:mm:ss,FFFFFFF",
		"yyyy-MM-dd HH:mm:ss,FFFFFFF",
		"yyyy.MM.dd HH:mm:ss,FFFFFFF",
		"yyyy/MM/dd HH:mm:ss,FFFFFFF",
		"yyyy MM dd HH:mm:ss,FFFFFFF",
		"yyyy:MM:ddTHH:mm:ss,FFFFFFF",
		"yyyy-MM-ddTHH:mm:ss,FFFFFFF",
		"yyyy.MM.ddTHH:mm:ss,FFFFFFF",
		"yyyy/MM/ddTHH:mm:ss,FFFFFFF",
		"yyyy MM ddTHH:mm:ss,FFFFFFF",

		"yyyy:MM:dd HH:mm:ss",
		"yyyy-MM-dd HH:mm:ss",
		"yyyy.MM.dd HH:mm:ss",
		"yyyy/MM/dd HH:mm:ss",
		"yyyy MM dd HH:mm:ss",
		"yyyy:MM:ddTHH:mm:ss",
		"yyyy-MM-ddTHH:mm:ss",
		"yyyy.MM.ddTHH:mm:ss",
		"yyyy/MM/ddTHH:mm:ss",
		"yyyy MM ddTHH:mm:ss",

		"yyyy:MM:dd HH:mm",
		"yyyy-MM-dd HH:mm",
		"yyyy.MM.dd HH:mm",
		"yyyy/MM/dd HH:mm",
		"yyyy MM dd HH:mm",
		"yyyy:MM:ddTHH:mm",
		"yyyy-MM-ddTHH:mm",
		"yyyy.MM.ddTHH:mm",
		"yyyy/MM/ddTHH:mm",
		"yyyy MM ddTHH:mm",

		"yyyyMMdd HH:mm:ss.FFFFFFF",
		"yyyyMMddTHH:mm:ss.FFFFFFF",
		"yyyyMMdd HH:mm:ss,FFFFFFF",
		"yyyyMMddTHH:mm:ss,FFFFFFF",

		"yyyyMMdd HH:mm:ss",
		"yyyyMMddTHH:mm:ss",

		"yyyyMMdd HH:mm",
		"yyyyMMddTHH:mm"
	};

	public override bool TryParse(string? raw, [NotNullWhen(true)] out DateTime result)
	{
		result = default;

		if(string.IsNullOrWhiteSpace(raw))
		{
			return false;
		}

		raw = raw.Trim();

		// Forsøg med alle formater
		return DateTime.TryParseExact(
			raw,
			DateTimeFormats,
			CultureInfo.InvariantCulture,
			DateTimeStyles.None,
			out result
		);
	}
}
