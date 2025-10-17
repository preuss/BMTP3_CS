using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Consoles.ConsoleCommands;
public class VerifyConsoleCommand : BaseConsoleCommand {
	public GlobalOptionsModel GlobalOptions { get; }
	public VerifyOptionsModel VerifyOptions { get; }

	public VerifyConsoleCommand() : this("verify", "Verificér backup", new GlobalOptionsModel(), new VerifyOptionsModel()) {
	}
	public VerifyConsoleCommand(
		string name, 
		string description, 
		GlobalOptionsModel globalOptionsModel,
		VerifyOptionsModel verifyOptionsModel
	) : base(name, description, globalOptionsModel, verifyOptionsModel) {
		GlobalOptions = globalOptionsModel;
		VerifyOptions = verifyOptionsModel;
	}

	protected override async Task<int> DoExecuteAsync(
		ParseResult parseResult,
		CancellationToken cancellationToken
	)
	{
		Console.WriteLine("Verificering udføres...");
		await Task.Delay(1000, cancellationToken);
		return 0;
	}
}