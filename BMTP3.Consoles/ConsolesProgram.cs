using BMTP3.Consoles.ConsoleCommands;
using BMTP3.Consoles.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.CommandLine;
using System.CommandLine.Parsing;

namespace BMTP3.Consoles;

public class ConsolesProgram {
	public static async Task<int> Main(string[] args)
	{
		args = ["backup", "--path", "C:\\BackupFolder"];
		args = ["backup", "asdf", "-unknown", "--help"];
		args = ["backup", "-v", "true", "true", "-v", "false", "false", "false", "-vvvv", "-v", "-v", "--help"];
		args = ["backup", "-v", "-v", "-v", "-v", "-v", "--delay=45"];

		IServiceProvider serviceProvider = ApplicationStartup.InitializeServiceProvider(args);

		var app = serviceProvider.GetRequiredService<ConsoleApplication>();

		var rootCommand = new RootCommand("BMTP3 CLI");


		GlobalOptionsModel globalOptions = new GlobalOptionsModel();
		globalOptions.GetAllOptions().ForEach(option => rootCommand.Options.Add(option));

		BackupConsoleCommand backupCommand = new() { ServiceProvider = serviceProvider};
		rootCommand.Subcommands.Add(backupCommand);

		VerifyConsoleCommand verifyCommand = new();
		rootCommand.Subcommands.Add(verifyCommand);

		/*
		Option<FileInfo> fileOption = new("--file") {
			Description = "The file to read and display on the console."
		};
		
		Option<int> delayOption = new("--delay") {
			Description = "Delay between lines, specified as milliseconds per character in a line.",
			DefaultValueFactory = parseResult => 42
		};
		Option<ConsoleColor> fgcolorOption = new("--fgcolor") {
			Description = "Foreground color of text displayed on the console.",
			DefaultValueFactory = parseResult => ConsoleColor.White
		};
		Option<bool> lightModeOption = new("--light-mode") {
			Description = "Background color of text displayed on the console: default is black, light mode is white."
		};
		Command c = new("read", "Read and display the file."){
			fileOption,
			delayOption,
			//fgcolorOption,
			lightModeOption
		};
		*/

		ParseResult parseResult= rootCommand.Parse(args);
		await parseResult.InvokeAsync();
		//await app.RunAsync();
		//await Task.CompletedTask;
		return 0;
	}
}