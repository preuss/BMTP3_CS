using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using System.Reflection;

namespace BMTP3.Consoles.ConsoleCommands;
public class GlobalOptionsModel : BaseOptionsModel
{
	public static Option<bool> VerboseOption { get; } = new("--verbose", "-v") { Description = "Enable verbose output. Repeat for more detail."};
	public static Func<ParseResult, int> ParseVerboseOption => result => result.GetResult(VerboseOption)?.IdentifierTokenCount ?? 0;
	public int Verbose { get; set; }
}
