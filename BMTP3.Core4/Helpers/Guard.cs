using System.Runtime.CompilerServices;

namespace BMTP3.Core4.Helpers;

internal static class Guard
{
	public static T RequireNonNull<T>(T? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
	{
		if(argument is null) throw new ArgumentNullException(paramName);

		return argument;
	}

	public static string RequireNonNullOrNonWhitespace(string? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
	{
		ArgumentNullException.ThrowIfNullOrWhiteSpace(argument, paramName);

		return argument;
	}

	public static long RequireZeroOrGreater(long? number, [CallerArgumentExpression(nameof(number))] string? paramName = null)
	{
		if(number is null) throw new ArgumentNullException(paramName);
		if(number < 0) throw new ArgumentOutOfRangeException(paramName, "Value must be zero or greater.");

		return number.Value;
	}
}