using System.CommandLine;

namespace BMTP3.Consoles.ConsoleCommands;

public class RepeatableFlagOption : Option<int>
{
	public RepeatableFlagOption(string name, params string[] aliases) : base(name, aliases)
	{
		Arity = ArgumentArity.Zero;
		CustomParser = argumentResult => argumentResult.GetResult(this)?.IdentifierTokenCount ?? 0;
	}
}