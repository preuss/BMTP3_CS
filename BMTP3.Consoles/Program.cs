using BMTP3.Consoles.ConsoleCommands;
using BMTP3.Consoles.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.CommandLine;
using System.CommandLine.Parsing;

namespace BMTP3.Consoles;

public class Program {
	public static async Task<int> Main(string[] args)
	{
		args = ["backup", "--path", "C:\\BackupFolder"];
		args = ["backup", "asdf", "-unknown", "--help"];
		args = ["backup", "-v", "true", "true", "-v", "false", "false", "false", "-vvvv", "-v", "-v", "--help"];
		args = ["backup", "-v", "-v", "-v", "-v", "-v"];

		IServiceProvider serviceProvider = ApplicationStartup.CreateConfiguration(args);

		var app = serviceProvider.GetRequiredService<ConsoleApplication>();

		var rootCommand = new RootCommand("BMTP3 CLI");


		GlobalOptionsModel globalOptions = new GlobalOptionsModel();
		globalOptions.GetAllOptions().ForEach(option => rootCommand.Options.Add(option));

		BackupConsoleCommand backupCommand = new();
		rootCommand.Subcommands.Add(backupCommand);

		VerifyConsoleCommand verifyCommand = new();
		rootCommand.Subcommands.Add(verifyCommand);

		ParseResult parseResult= rootCommand.Parse(args);
		await parseResult.InvokeAsync();
		//await app.RunAsync();
		await Task.CompletedTask;
		return 0;
	}
}
