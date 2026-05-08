using System;
using System.Runtime.CompilerServices;

namespace BMTP3.Core4.Helpers;

internal static class Guard
{
	public static T RequireNonNull<T>(T? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
	{
		if(argument is null) throw new ArgumentNullException(paramName);
		return argument;
	}
}