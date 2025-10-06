using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Consoles.ConsoleCommands;
public class BackupConsoleCommand : Command {
	public BackupConsoleCommand() : base("backup", "Udfør backup") {
		SetAction(async (ParseResult parseResult, CancellationToken cancellationToken) => {
			await Task.Delay(100, cancellationToken);
			Console.WriteLine("Backup udført!");
		});
	}
}

