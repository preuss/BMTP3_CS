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
		//args = ["verify", "-v", "-v", "-v", "-v", "-v", "-d", "--help"];
		//args = ["verify", "-v", "-v", "-v", "-v", "-v", "-d"];
		//args = ["verify", "--help"];
		//TestMultipleCommands(args);
		//return 0;

		IServiceProvider serviceProvider = ApplicationStartup.InitializeServiceProvider(args);

		var app = serviceProvider.GetRequiredService<ConsoleApplication>();

		var rootCommand = new RootCommand("BMTP3 CLI");


		GlobalOptionsModel globalOptions = new GlobalOptionsModel();
		globalOptions.GetAllOptions().ForEach(option => rootCommand.Options.Add(option));

		BackupConsoleCommand backupCommand = new() { ServiceProvider = serviceProvider};
		rootCommand.Subcommands.Add(backupCommand);

		VerifyConsoleCommand verifyCommand = new();
		rootCommand.Subcommands.Add(verifyCommand);

		var o = rootCommand.Options;
		Console.WriteLine($"Options i root Command: " + o.Count);
		for (int i = 0; i < o.Count; i++)
		{
			var option = o[i];
			Console.WriteLine(option);
		}

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

	private static void TestMultipleCommands(string[] args)
	{
		args = ["cmd1", "--device", "DeviceA"];
		args = ["cmd2", "--device", "DeviceB"];
		args = ["cmd3", "--device", "DeviceC"];
		args = ["cmd2", "-d", "TestDevice"];
		args = ["--help"];
		args = ["cmd1", "--help"];

		// Opret root command
		var rootCommand = new RootCommand("Test CLI");

		// Opret første command med option --device/-d
		var cmd1 = new Command("cmd1", "Command 1");
		var deviceOption1 = new Option<string>("--device", "-d") { Description = "Device for cmd1" };
		cmd1.Add(deviceOption1);

		// Opret anden command med option --device/-d
		var cmd2 = new Command("cmd2", "Command 2");
		var deviceOption2 = new Option<string>("--device", "-d") { Description = "Device for cmd2" };
		cmd2.Add(deviceOption2);

		// Opret tredje command med option --device/-d
		var cmd3 = new Command("cmd3", "Command 3");
		var deviceOption3 = new Option<string>("--device", "-d") { Description = "Device for cmd3" };
		cmd3.Add(deviceOption3);

		// Tilføj commands til root
		rootCommand.Add(cmd1);
		rootCommand.Add(cmd2);
		rootCommand.Add(cmd3);

		// Sæt handler for hver command
		cmd1.SetAction((parseResult) => {
			var device = parseResult.GetValue(deviceOption1);
			Console.WriteLine($"cmd1 device: {device}");
			return 0;
		});

		cmd2.SetAction((parseResult) => {
			var device = parseResult.GetValue(deviceOption2);
			Console.WriteLine($"cmd2 device: {device}");
			return 0;
		});

		cmd3.SetAction((parseResult) => {
			var device = parseResult.GetValue(deviceOption3);
			Console.WriteLine($"cmd3 device: {device}");
			return 0;
		});

		// Eksempel på test-args: ["cmd2", "--device", "TestDevice"]
		var parseResult = rootCommand.Parse(args);
		parseResult.Invoke();
	}
}