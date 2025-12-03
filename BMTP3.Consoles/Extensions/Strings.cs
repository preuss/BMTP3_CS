namespace BMTP3.Consoles.Extensions;
public static class Strings
{
	public static string? ToNullIfNullOrWhiteSpace(this string? str)
	{
		if(String.IsNullOrWhiteSpace(str))
		{
			return null;
		} else
		{
			return str;
		}
	}
}
