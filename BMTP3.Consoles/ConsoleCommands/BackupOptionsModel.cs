using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Consoles.ConsoleCommands;
public class BackupOptionsModel : BaseOptionsModel
{
	public static Option<int> DelayOption { get; } = new("--delay"){ 
		Description = "Delay between lines, specified as milliseconds per character in a line.", 
		DefaultValueFactory = parseResult => 42,
		Arity = ArgumentArity.ZeroOrOne
	};
	public int Delay { get; set; }
}
