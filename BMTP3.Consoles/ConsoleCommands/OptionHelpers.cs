using System.CommandLine;
using System.CommandLine.Parsing;

namespace BMTP3.Consoles.ConsoleCommands;

internal static class OptionHelpers
{
	public static bool WasSupplied<T>(ParseResult parseResult, Option<T> option)
	{
		OptionResult? result = parseResult.GetResult(option);
		return result is not null && !result.Implicit;
	}
}
