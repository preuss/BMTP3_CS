using BMTP3.Consoles.ConsoleCommands;
using BMTP3.Consoles.exifreader;
using BMTP3.Consoles.Startup.Configurations;
using BMTP3.Consoles.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.CommandLine;
using System.Globalization;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;
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

	public static void ApplyServiceSetups(IServiceCollection services, IEnumerable<IServiceSetup> setups, IConfiguration configuration)
	{
		foreach(IServiceSetup setup in setups)
		{
			setup.Configure(services, configuration);
		}
	}
	public static async Task<int> Main(string[] args)
	{
		// Shortcurcuit to test Exif reader:
		//new ExifReader().ReadExifData("C:\\Private.Testing\\test.source\\iPhone14\\202212__\\IMG_2196.HEIC");
		ExifReader2 exifReader = new ExifReader2();
		//exifReader.ReadExifData("D:\\Projects.Github\\sample-meta-data-media\\IMG_2205.JPG");
		List<string> files = [
			"D:\\Projects.Github\\sample-meta-data-media\\exif-samples\\jpg\\tests\\32-lens_data.jpeg",
			"D:\\Projects.Github\\sample-meta-data-media\\exif-samples\\jpg\\hdr\\canon_hdr_YES.jpg",
			"D:\\Projects.Github\\sample-meta-data-media\\exif-samples\\jpg\\invalid\\image00971.jpg",
			"D:\\Projects.Github\\sample-meta-data-media\\exif-samples\\jpg\\mobile\\HMD_Nokia_8.3_5G.jpg",
			"D:\\Projects.Github\\sample-meta-data-media\\metadata-extractor-images\\heic\\IMG_1034.heic",
			"D:\\Projects.Github\\sample-meta-data-media\\metadata-extractor-images\\heic\\IMG_2927.HEIC",
			"D:\\Projects.Github\\sample-meta-data-media\\metadata-extractor-images\\mov\\apple-livephoto-quicktime.mov",
			"D:\\Projects.Github\\sample-meta-data-media\\metadata-extractor-images\\png\\sampleWithExifData.png",
			"D:\\Projects.Github\\sample-meta-data-media\\metadata-extractor-images\\png\\Issue 316 (dotnet).png",
		];
		foreach(string file in files)
		{
			Console.WriteLine($"\n--- Reading EXIF data for file: {file} ---");
			//exifReader.ReadExifData(file);
		}
		Console.WriteLine($"\n--- Reading EXIF data for file: {files[7]} ---");
		//exifReader.ReadExifData(files[7], false);

		//string iso = "2026-01-04T12:56:12+04:00";
		//string iso = "2026-01-04T12:56:12Z";
		string iso = "2025-06-15T12:30:00+08:00";
		DateTime dt = DateTime.Parse(iso, null, DateTimeStyles.RoundtripKind);
		Console.WriteLine("Dt: " + dt.ToString("o"));  // Output: 2025-06-15T04:30:00.0000000Z
		Console.WriteLine("Print: " + dt.ToString());     // Output: 6/15/2025 4:30:00 AM
		Console.WriteLine("Kind: " + dt.Kind);           // Output: Utc
		Console.WriteLine("UTC: " + dt.ToUniversalTime().ToString("o"));

		return 0;
		// Temporary test args
		args = ["backup", "--path", "C:\\BackupFolder"];
		args = ["backup", "asdf", "-unknown", "--help"];
		args = ["backup", "-v", "true", "true", "-v", "false", "false", "false", "-vvvv", "-v", "-v", "--help"];
		args = ["backup", "-v", "-v", "-v", "-v", "-v", "--delay=45"];
		//args = ["verify", "-v", "-v", "-v", "-v", "-v", "-d", "--help"];
		//args = ["verify", "-v", "-v", "-v", "-v", "-v", "-d"];
		args = ["backup", "", "--output-structure=xxx", "--help"];
		args = ["backup", "--config=default.toml", "--output-structure=PreserveSourceTree"];
		args = ["backupTest", "--config=default.toml", "--output-structure=PreserveSourceTree"];

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

		//IServiceProvider serviceProvider = ApplicationStartup.InitializeServiceProvider(args);
		IServiceProvider serviceProvider = host.Services;

		ConsoleApplication app = serviceProvider.GetRequiredService<ConsoleApplication>();

		RootCommand rootCommand = new RootCommand("BMTP3 CLI");


		GlobalOptionsModel globalOptions = new GlobalOptionsModel();
		globalOptions.GetAllOptions().ForEach(option => rootCommand.Options.Add(option));

		BackupConsoleCommand backupCommand = new() { ServiceProvider = serviceProvider };
		rootCommand.Subcommands.Add(backupCommand);

		BackupTestConsoleCommand backupTestCommand = new() { ServiceProvider = serviceProvider };
		rootCommand.Subcommands.Add(backupTestCommand);

		VerifyConsoleCommand verifyCommand = new();
		rootCommand.Subcommands.Add(verifyCommand);

		IList<Option> o = rootCommand.Options;
		Console.WriteLine($"Options i root Command: " + o.Count);
		foreach(Option option in o)
		{
			Console.WriteLine(option);
		}


		ParseResult parseResult = rootCommand.Parse(args);
		return await parseResult.InvokeAsync();
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
		cmd1.SetAction((parseResult) =>
		{
			string? device = parseResult.GetValue(deviceOption1);
			Console.WriteLine($"cmd1 device: {device}");
			return 0;
		});

		cmd2.SetAction((parseResult) =>
		{
			string? device = parseResult.GetValue(deviceOption2);
			Console.WriteLine($"cmd2 device: {device}");
			return 0;
		});

		cmd3.SetAction((parseResult) =>
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

		Console.WriteLine($"--- Tester DotNet.Glob ---");
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
			string regexPattern = GlobConverter.GlobToRegex(globPattern);
			Console.WriteLine("Regex Pattern: " + regexPattern);
			Regex regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
			actualMatch = regex.Match(pathConverted).Success;

			string status = actualMatch == testCase.Expected ? "✅ OK" : "❌ FEJL";

			Console.WriteLine(string.Format(
				"{0,-" + padding + "} {1,-10} {2,-10} {3,-10}",
				testCase.Path, testCase.Expected, actualMatch, status
			));
		}
	}
}