using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.exifreader.parsers;

public class DateTimeOffsetParser : ParserBase<DateTimeOffset>
{
	private static readonly string[] DateTimeOffsetFormats =
	{
		"yyyy:MM:dd HH:mm:ss.FFFFFFFzzz",
		"yyyy-MM-dd HH:mm:ss.FFFFFFFzzz",
		"yyyy.MM.dd HH:mm:ss.FFFFFFFzzz",
		"yyyy/MM/dd HH:mm:ss.FFFFFFFzzz",
		"yyyy MM dd HH:mm:ss.FFFFFFFzzz",
		"yyyy:MM:ddTHH:mm:ss.FFFFFFFzzz",
		"yyyy-MM-ddTHH:mm:ss.FFFFFFFzzz",
		"yyyy.MM.ddTHH:mm:ss.FFFFFFFzzz",
		"yyyy/MM/ddTHH:mm:ss.FFFFFFFzzz",
		"yyyy MM ddTHH:mm:ss.FFFFFFFzzz",

		"yyyy:MM:dd HH:mm:ss,FFFFFFFzzz",
		"yyyy-MM-dd HH:mm:ss,FFFFFFFzzz",
		"yyyy.MM.dd HH:mm:ss,FFFFFFFzzz",
		"yyyy/MM/dd HH:mm:ss,FFFFFFFzzz",
		"yyyy MM dd HH:mm:ss,FFFFFFFzzz",
		"yyyy:MM:ddTHH:mm:ss,FFFFFFFzzz",
		"yyyy-MM-ddTHH:mm:ss,FFFFFFFzzz",
		"yyyy.MM.ddTHH:mm:ss,FFFFFFFzzz",
		"yyyy/MM/ddTHH:mm:ss,FFFFFFFzzz",
		"yyyy MM ddTHH:mm:ss,FFFFFFFzzz",

		"yyyy:MM:dd HH:mm:sszzz",
		"yyyy-MM-dd HH:mm:sszzz",
		"yyyy.MM.dd HH:mm:sszzz",
		"yyyy/MM/dd HH:mm:sszzz",
		"yyyy MM dd HH:mm:sszzz",
		"yyyy:MM:ddTHH:mm:sszzz",
		"yyyy-MM-ddTHH:mm:sszzz",
		"yyyy.MM.ddTHH:mm:sszzz",
		"yyyy/MM/ddTHH:mm:sszzz",
		"yyyy MM ddTHH:mm:sszzz",

		"yyyy:MM:dd HH:mmzzz",
		"yyyy-MM-dd HH:mmzzz",
		"yyyy.MM.dd HH:mmzzz",
		"yyyy/MM/dd HH:mmzzz",
		"yyyy MM dd HH:mmzzz",
		"yyyy:MM:ddTHH:mmzzz",
		"yyyy-MM-ddTHH:mmzzz",
		"yyyy.MM.ddTHH:mmzzz",
		"yyyy/MM/ddTHH:mmzzz",
		"yyyy MM ddTHH:mmzzz",

		"yyyyMMdd HH:mm:ss.FFFFFFFzzz",
		"yyyyMMddTHH:mm:ss.FFFFFFFzzz",

		"yyyyMMdd HH:mm:ss,FFFFFFFzzz",
		"yyyyMMddTHH:mm:ss,FFFFFFFzzz",

		"yyyyMMdd HH:mm:sszzz",
		"yyyyMMddTHH:mm:sszzz",

		"yyyyMMdd HH:mmzzz",
		"yyyyMMddTHH:mmzzz",

		"yyyy:MM:dd HH:mm:ss.FFFFFFFK",
		"yyyy-MM-dd HH:mm:ss.FFFFFFFK",
		"yyyy.MM.dd HH:mm:ss.FFFFFFFK",
		"yyyy/MM/dd HH:mm:ss.FFFFFFFK",
		"yyyy MM dd HH:mm:ss.FFFFFFFK",
		"yyyy:MM:ddTHH:mm:ss.FFFFFFFK",
		"yyyy-MM-ddTHH:mm:ss.FFFFFFFK",
		"yyyy.MM.ddTHH:mm:ss.FFFFFFFK",
		"yyyy/MM/ddTHH:mm:ss.FFFFFFFK",
		"yyyy MM ddTHH:mm:ss.FFFFFFFK",

		"yyyy:MM:dd HH:mm:ss,FFFFFFFK",
		"yyyy-MM-dd HH:mm:ss,FFFFFFFK",
		"yyyy.MM.dd HH:mm:ss,FFFFFFFK",
		"yyyy/MM/dd HH:mm:ss,FFFFFFFK",
		"yyyy MM dd HH:mm:ss,FFFFFFFK",
		"yyyy:MM:ddTHH:mm:ss,FFFFFFFK",
		"yyyy-MM-ddTHH:mm:ss,FFFFFFFK",
		"yyyy.MM.ddTHH:mm:ss,FFFFFFFK",
		"yyyy/MM/ddTHH:mm:ss,FFFFFFFK",
		"yyyy MM ddTHH:mm:ss,FFFFFFFK",

		"yyyy:MM:dd HH:mm:ssK",
		"yyyy-MM-dd HH:mm:ssK",
		"yyyy.MM.dd HH:mm:ssK",
		"yyyy/MM/dd HH:mm:ssK",
		"yyyy MM dd HH:mm:ssK",
		"yyyy:MM:ddTHH:mm:ssK",
		"yyyy-MM-ddTHH:mm:ssK",
		"yyyy.MM.ddTHH:mm:ssK",
		"yyyy/MM/ddTHH:mm:ssK",
		"yyyy MM ddTHH:mm:ssK",

		"yyyy:MM:dd HH:mmK",
		"yyyy-MM-dd HH:mmK",
		"yyyy.MM.dd HH:mmK",
		"yyyy/MM/dd HH:mmK",
		"yyyy MM dd HH:mmK",
		"yyyy:MM:ddTHH:mmK",
		"yyyy-MM-ddTHH:mmK",
		"yyyy.MM.ddTHH:mmK",
		"yyyy/MM/ddTHH:mmK",
		"yyyy MM ddTHH:mmK",

		"yyyyMMdd HH:mm:ss.FFFFFFFK",
		"yyyyMMddTHH:mm:ss.FFFFFFFK",

		"yyyyMMdd HH:mm:ss,FFFFFFFK",
		"yyyyMMddTHH:mm:ss,FFFFFFFK",

		"yyyyMMdd HH:mm:ssK",
		"yyyyMMddTHH:mm:ssK",

		"yyyyMMdd HH:mmK",
		"yyyyMMddTHH:mmK",
	};

	public override bool TryParse(string? raw, [NotNullWhen(true)] out DateTimeOffset result)
	{
		result = default;

		if(string.IsNullOrWhiteSpace(raw))
			return false;

		raw = raw.Trim();

		// Forsøg med alle formater
		return DateTimeOffset.TryParseExact(
			raw,
			DateTimeOffsetFormats,
			CultureInfo.InvariantCulture,
			DateTimeStyles.None,
			out result
		);
	}
}