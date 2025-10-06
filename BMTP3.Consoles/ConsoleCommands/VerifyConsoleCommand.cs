using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Consoles.ConsoleCommands;
public class VerifyConsoleCommand : Command {
	public VerifyConsoleCommand() : base("verify", "Verificér backup") {
		SetAction((ParseResult parseResult) => {
			Console.WriteLine("Verificering udføres...");
			// Her kan du kalde din verify-logik
		});
	}
}