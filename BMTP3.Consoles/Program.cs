using BMTP3.Consoles.ConsoleCommands;
using BMTP3.Consoles.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.CommandLine;

namespace BMTP3.Consoles;

public class Program {
	public static async Task<int> Main(string[] args)
	{
		args = ["backup", "--path", "C:\\BackupFolder"];
		args = ["backup", "asdf", "-unknown", "--help"];
		args = ["-v", "-v", "-v", "-v", "-v", "--help"];
		args = ["backup", "-v", "-v", "-v", "-v", "-v"];

		IServiceProvider serviceProvider = ApplicationStartup.CreateConfiguration(args);

		var app = serviceProvider.GetRequiredService<ConsoleApplication>();

		var rootCommand = new RootCommand("BMTP3 CLI");

		var verboseOption = new Option<int>("verbose")
		{
			Description = "Enable verbose output. Repeat for more detail.",
			Arity = ArgumentArity.ZeroOrMore
		};
		verboseOption.Aliases.Add("-v");
		verboseOption.Aliases.Add("--verbose");
		verboseOption.CustomParser = argumentResult => argumentResult.Tokens.Count;



		rootCommand.Add(verboseOption);
		rootCommand.Add(new BackupConsoleCommand() {Options = { verboseOption }});
		rootCommand.Add(new VerifyConsoleCommand());

		var parseResult= rootCommand.Parse(args);
		await parseResult.InvokeAsync();
		//await app.RunAsync();
		await Task.CompletedTask;
		return 0;
	}
}
