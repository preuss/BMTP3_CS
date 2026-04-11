namespace BMTP3.Consoles.Extensions;

public static class Strings
{
	public static string? ToNullIfNullOrWhiteSpace(this string? str)
	{
		if (string.IsNullOrWhiteSpace(str))
		{
			return null;
		}

		return str;
	}
}