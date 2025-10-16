using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Consoles.ConsoleCommands;
public class RepeatableFlagOption : Option<int>
{
	public RepeatableFlagOption(string name, params string[] aliases) : base(name, aliases)
	{
		Arity = ArgumentArity.ZeroOrMore;
		CustomParser = argumentResult =>
		{
			return argumentResult.GetResult(this)?.IdentifierTokenCount ?? 0;
		};
	}
}
