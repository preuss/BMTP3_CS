using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Consoles.ConsoleCommands;
public class VerifyConsoleCommand : BaseConsoleCommand<GlobalOptionsModel, VerifyOptionsModel> {
	public VerifyConsoleCommand() : base("verify", "Verificér backup") {
	}

	protected override async Task<int> DoCommandAsync(
		GlobalOptionsModel globalOptionsModel, 
		VerifyOptionsModel optionsModel, 
		ParseResult parseResult,
		CancellationToken cancellationToken
	)
	{
		Console.WriteLine("Verificering udføres...");
		await Task.Delay(1000, cancellationToken);
		return 0;
	}
}