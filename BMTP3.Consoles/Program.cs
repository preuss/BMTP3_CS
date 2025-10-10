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

		var verboseOption = new Option<bool>("--verbose")
		{
			Description = "Enable verbose output. Repeat for more detail.",
		};
		verboseOption.Aliases.Add("-v");
		//verboseOption.Aliases.Add("--verbose");
		//verboseOption.CustomParser = argumentResult => argumentResult.Tokens.Count;


		rootCommand.Options.Add(verboseOption);
		rootCommand.Subcommands.Add(new BackupConsoleCommand() {Options = { verboseOption }});
		rootCommand.Subcommands.Add(new VerifyConsoleCommand());

		ParseResult parseResult= rootCommand.Parse(args);
		OptionResult? or = parseResult.GetResult(verboseOption);
		if(or != null)
		{
			Console.WriteLine(or.IdentifierTokenCount);
		}
		await parseResult.InvokeAsync();
		//await app.RunAsync();
		await Task.CompletedTask;
		return 0;
	}
}
