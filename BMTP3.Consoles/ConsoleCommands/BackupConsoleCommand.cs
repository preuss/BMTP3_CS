using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace BMTP3.Consoles.ConsoleCommands;

public class BackupConsoleCommand : BaseConsoleCommand<GlobalOptionsModel, BackupOptionsModel>
{
	public BackupConsoleCommand() : base("backup", "Perform backup") { }

	/// <summary>
	/// Entry point for the backup command action.
	/// </summary>
	protected override async Task<int> DoExecuteAsync(
		GlobalOptionsModel globalOptionsModel, 
		BackupOptionsModel optionsModel, 
		ParseResult parseResult, 
		CancellationToken cancellationToken
	)
	{
		int verbosity = globalOptionsModel.Verbose;
		
		Console.WriteLine("Verbose Level: " + verbosity);
		Console.WriteLine("Delay: " + optionsModel.Delay);

		// Simulates backup work here.
		await Task.Delay(100, cancellationToken);
		// Add your actual backup logic here

		PrintResult();
		return 0;
	}

	/// <summary>
	/// Prints the result of the backup operation.
	/// </summary>
	private void PrintResult()
	{
		Console.WriteLine("Backup completed!");
	}
}