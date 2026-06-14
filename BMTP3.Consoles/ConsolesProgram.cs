using BMTP3.Common.Utilities;
using BMTP3.Consoles.ConsoleCommands;
using BMTP3.Consoles.Startup.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.CommandLine;
using System.CommandLine.Help;
using System.Text.RegularExpressions;
using DGlob = DotNet.Globbing;

namespace BMTP3.Consoles;

public class ConsolesProgram
{
	public static void ApplyConfigSetups(IConfigurationManager configuration, IEnumerable<IConfigSetup> setups)
	{
		foreach(IConfigSetup setup in setups)
		{
			setup.Configure(configuration);
		}
	}

	public static void ApplyServiceSetups(IServiceCollection services, IEnumerable<IServiceSetup> setups,
		IConfiguration configuration)
	{
		foreach(IServiceSetup setup in setups)
		{
			setup.Configure(services, configuration);
		}
	}

	public static async Task<int> Main(string[] args)
	{
		HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
		List<IConfigSetup> configSetups = new()
		{
			new ConfigAppSetup()
		};
		ApplyConfigSetups(builder.Configuration, configSetups);

		List<IServiceSetup> serviceSetups = new()
		{
			new LoggingServiceSetup(),
			new ConsolesServiceSetup(),
			new ApplicationServiceSetup()
		};
		ApplyServiceSetups(builder.Services, serviceSetups, builder.Configuration);

		IHost host = builder.Build();
		IServiceProvider serviceProvider = host.Services;

		RootCommand rootCommand = new("BMTP3 CLI");

		GlobalOptionsModel globalOptions = new();
		globalOptions.GetAllOptions().ForEach(option => rootCommand.Options.Add(option));

		BackupConsoleCommand2 backupCommand2 = new() { ServiceProvider = serviceProvider };
		rootCommand.Subcommands.Add(backupCommand2);

		BackupTestConsoleCommand backupTestCommand = new() { ServiceProvider = serviceProvider };
		rootCommand.Subcommands.Add(backupTestCommand);

		VerifyConsoleCommand verifyCommand = new() { ServiceProvider = serviceProvider };
		rootCommand.Subcommands.Add(verifyCommand);

		// Command 4 - Newest version with improved options and features
		BackupConsoleCommand4 backupCommand4 = new() { ServiceProvider = serviceProvider };
		rootCommand.Subcommands.Add(backupCommand4);

		BackupConsoleCommand4ListDrives listDrivesCommand4 = new() { ServiceProvider = serviceProvider };
		rootCommand.Subcommands.Add(listDrivesCommand4);

		BackupConsoleCommand4InitConfig initConfigCommand4 = new() { ServiceProvider = serviceProvider };
		rootCommand.Subcommands.Add(initConfigCommand4);

		//ReplaceHelp(rootCommand);

		ParseResult parseResult = rootCommand.Parse(args);
		return await parseResult.InvokeAsync();
	}

	private static void ReplaceHelp(Command command)
	{
		HelpOption? old = command.Options
			.OfType<HelpOption>()
			.FirstOrDefault();

		if(old != null)
		{
			command.Options.Remove(old);
		}

		command.Add(new HelpOption("-h", "--help")
		{
			Description = "Show help and usage information",
			Action = new CustomHelpAction()
		});

		foreach(Command sub in command.Subcommands)
		{
			ReplaceHelp(sub);
		}
	}

	private static void TestMultipleCommands(string[] args)
	{
		/*
		args = ["cmd1", "--device", "DeviceA"];
		args = ["cmd2", "--device", "DeviceB"];
		args = ["cmd3", "--device", "DeviceC"];
		args = ["cmd2", "-d", "TestDevice"];
		args = ["--help"];
		args = ["cmd1", "--help"];
		*/

		// Opret root command
		RootCommand rootCommand = new("Test CLI");

		// Opret første command med option --device/-d
		Command cmd1 = new("cmd1", "Command 1");
		Option<string> deviceOption1 = new("--device", "-d") { Description = "Device for cmd1" };
		cmd1.Add(deviceOption1);

		// Opret anden command med option --device/-d
		Command cmd2 = new("cmd2", "Command 2");
		Option<string> deviceOption2 = new("--device", "-d") { Description = "Device for cmd2" };
		cmd2.Add(deviceOption2);

		// Opret tredje command med option --device/-d
		Command cmd3 = new("cmd3", "Command 3");
		Option<string> deviceOption3 = new("--device", "-d") { Description = "Device for cmd3" };
		cmd3.Add(deviceOption3);

		// Tilføj commands til root
		rootCommand.Add(cmd1);
		rootCommand.Add(cmd2);
		rootCommand.Add(cmd3);

		// Sæt handler for hver command
		cmd1.SetAction(parseResult =>
		{
			string? device = parseResult.GetValue(deviceOption1);
			Console.WriteLine($"cmd1 device: {device}");
			return 0;
		});

		cmd2.SetAction(parseResult =>
		{
			string? device = parseResult.GetValue(deviceOption2);
			Console.WriteLine($"cmd2 device: {device}");
			return 0;
		});

		cmd3.SetAction(parseResult =>
		{
			string? device = parseResult.GetValue(deviceOption3);
			Console.WriteLine($"cmd3 device: {device}");
			return 0;
		});

		// Eksempel på test-args: ["cmd2", "--device", "TestDevice"]
		ParseResult parseResult = rootCommand.Parse(args);
		parseResult.Invoke();
	}

	public static void RunFormattedTest(string globPatternInput)
	{
		var testCases = new[]
		{
			new { Path = "C:\\start\\anotherdir\\fileA.jpeg", Expected = true },
			new { Path = "C:\\start\\anotherdir\\fileB.jpeg", Expected = false },
			new { Path = "C:\\start\\anotherdir\\fileC.jpeg", Expected = true },
			new { Path = "C:\\start\\anotherdir\\fileAB.jpeg", Expected = false },
			new { Path = "C:\\start\\anotherdir\\file.jpeg", Expected = false }
		}.ToList();

		string globPattern = globPatternInput.Replace('\\', '/');
		//globPattern = globPatternInput;
		DGlob.Glob dglob = DGlob.Glob.Parse(globPattern);

		Console.WriteLine("--- Tester DotNet.Glob ---");
		Console.WriteLine($"Glob Pattern: {globPattern}");
		Console.WriteLine("--------------------------");

		int padding = testCases.Max(tc => tc.Path.Length) + 3;

		Console.WriteLine("{\"Sti\",-" + padding + "} {\"Forventet\",-10} {\"Faktisk\",-10} {\"Status\",-10}");
		Console.WriteLine(new string('-', padding + 30));

		foreach(var testCase in testCases)
		{
			string pathConverted = testCase.Path; // Use the actual path
			pathConverted = pathConverted.Replace('\\', '/');

			bool actualMatch = dglob.IsMatch(pathConverted);
			string regexPattern = GlobMatcher.GlobToRegex(globPattern);
			Console.WriteLine("Regex Pattern: " + regexPattern);
			Regex regex = new(regexPattern, RegexOptions.IgnoreCase);
			actualMatch = regex.Match(pathConverted).Success;

			string status = actualMatch == testCase.Expected ? "✅ OK" : "❌ FEJL";

			Console.WriteLine("{0,-" + padding + "} {1,-10} {2,-10} {3,-10}", testCase.Path, testCase.Expected,
				actualMatch, status);
		}
	}
}