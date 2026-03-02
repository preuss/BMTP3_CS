using System.Globalization;

namespace BMTP3.Consoles.exifreader;
public static class ExifDateTimeParser
{
	private static readonly string[] Formats =
	{
		"yyyy:MM:dd HH:mm:ss",
		"yyyy:MM:dd HH:mm",
		"yyyy:MM:dd"
	};

	public static bool TryParse(
		string raw,
		out DateOnly? date,
		out TimeOnly? time
	)
	{
		date = null;
		time = null;

		if(DateTime.TryParseExact(
			raw.Trim(),
			Formats,
			CultureInfo.InvariantCulture,
			DateTimeStyles.None,
			out DateTime dt))
		{
			date = DateOnly.FromDateTime(dt);
			time = TimeOnly.FromDateTime(dt);
			return true;
		}

		return false;
	}
}
