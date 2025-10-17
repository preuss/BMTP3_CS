using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace BMTP3.Consoles.ConsoleCommands;

public class BackupConsoleCommand : BaseConsoleCommand {
	public GlobalOptionsModel GlobalOptions { get; }
	public BackupOptionsModel BackupOptions { get; }
	public required IServiceProvider ServiceProvider { get; init; }

	public BackupConsoleCommand() : this("backup", "Perform backup", new GlobalOptionsModel(), new BackupOptionsModel()) { }

	private BackupConsoleCommand(
		string name, 
		string description, 
		GlobalOptionsModel globalOptionsModel, 
		BackupOptionsModel backupOptionsModel
	) : base(name, description, globalOptionsModel, backupOptionsModel)
	{
		GlobalOptions = globalOptionsModel;
		BackupOptions = backupOptionsModel;
	}

	/// <summary>
	/// Entry point for the backup command action.
	/// </summary>
	protected override async Task<int> DoExecuteAsync(
		ParseResult parseResult,
		CancellationToken cancellationToken
	) {
		int verbosity = GlobalOptions.Verbose;

		Console.WriteLine("Verbose Level: " + verbosity);
		Console.WriteLine("Delay: " + BackupOptions.Delay);

		//var backupMaster = ServiceProvider.GetService<BackupMaster>();

		// Simulates backup work here.
		await Task.Delay(100, cancellationToken);
		// Add your actual backup logic here

		PrintResult();
		return 0;
	}

	/// <summary>
	/// Prints the result of the backup operation.
	/// </summary>
	private void PrintResult() {
		Console.WriteLine("Backup completed!");
	}
}