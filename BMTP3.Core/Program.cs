using BMTP3.Core.Configs;
using BMTP3.Core.Configuration;
using BMTP3.Core.Handlers;
using BMTP3.Core.Handlers.EventHandlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using ZLogger;
using SystemConsole = System.Console;

namespace BMTP3.Core {
	[SupportedOSPlatform("windows10.0")]
	internal class Program {
		public static readonly DateTime MinWin32FileTime = DateTime.FromFileTimeUtc(0);

		public static readonly DateTime startedDateTime = DateTime.Now;

		private static readonly IAnsiConsole Console;

		private static readonly IServiceProvider serviceProvider;
		private static readonly IConfiguration configuration;

		private static readonly CancellationTokenSource cts;

		private static readonly ILogger globalLogger;// = LogManager.Logger;
		private static ILogger<Program> Logger;// = LogManager.GetLogger<Program>();
		private static TextWriter Message; // = LogManager.GetMessageWriter();


		static Program() {
			// Set console output encoding to UTF-8
			SystemConsole.OutputEncoding = Encoding.UTF8;

			Console = AnsiConsole.Create(new AnsiConsoleSettings());

			StartUp startUp = StartUp.CreateAndInitialize();
			serviceProvider = startUp.ServiceProvider;
			configuration = serviceProvider.GetService<IConfiguration>()!;

			cts = serviceProvider.GetService<CancellationTokenSource>()!;

			LogManager.Initialize(); // Force static initialize.
			globalLogger = LogManager.Logger;
			Logger = LogManager.GetLogger<Program>();
			Message = LogManager.GetMessageWriter();
		}
		private static bool RequestToQuit { get; set; }

		static async Task<int> Main(string[] args) {
#if DEBUG
			// Test only code here.
			const bool enableHashTesting = false;
			if(enableHashTesting) {
				FileInfo fileInfo = new FileInfo(@"c:\Priv2\xxx3.png");
				List<HashCalculator.HashType> hashTypes = [
					/*
						HashCalculator.HashType.SHA3_512_KECCAK,
						HashCalculator.HashType.SHA3_512_FIPS202,
						HashCalculator.HashType.SHA2_512,
						HashCalculator.HashType.SHA2_256,
						HashCalculator.HashType.MD5_128,
						HashCalculator.HashType.BLAKE3_256,
						HashCalculator.HashType.BLAKE3_512,*/
					HashCalculator.HashType.MD5_128,

			];

				var sw = Stopwatch.StartNew();

				IReadOnlyDictionary<HashCalculator.HashType, string> hashes = BackupHelper.ComputeHashes(fileInfo.FullName, hashTypes);

				if(fileInfo.Name.Equals("Big.png", StringComparison.OrdinalIgnoreCase)) {
					Console.WriteLine($"SideCar took {sw.ElapsedMilliseconds} ms for {fileInfo.Name}");
				}
				Console.WriteLine($"SideCar took {sw.ElapsedMilliseconds} ms for {fileInfo.Name}");
				foreach(var item in hashTypes) {
					Console.WriteLine("Hash for Big.png: " + item + ", hash: " + hashes[item]);
				}
				return 0;
			}


			// Only inject test arguments if none provided.
			if(args.Length == 0) {
				//args = ["--verify", "iPhone.toml"];
				//args = ["--backup", "iPhone.toml"];
				args = ["--backup", "TestAndroidBackup.toml"];
				//args = ["--backup", "TestLocalFolderBackup.toml"];
				//args = ["--verifyPath", ];
				Console.WriteLine($"Hello world");
			}
#endif
			foreach(var x in args) {
				Console.WriteLine(x);
			}

			await Task.Delay(1);

			//LogManager.Initialize(); // Force static initialize.

			globalLogger.ZLogCritical($"Application is starting.");

			Stopwatch stopwatch = Stopwatch.StartNew();

			Logger.ZLogTrace($"Start application");
			Console.WriteLine("Start application");
			int exitCode = ExitCodes.UnhandledError;
			using(cts) {
				ConsoleEventHandler.Initialize(cts);

				try {
					ConfigurationHandler configHandler = new ConfigurationHandler(
						args,
						configuration,
						serviceProvider.GetRequiredService<BackupSettingsReader>()!
					);
					Console.MarkupLine($"Config File brugt: [green]{configHandler.BackupSettings?.BackupConfigFile}[/]");

					RunCommand command = configHandler.Arguments.GetRunCommand();
					Console.WriteLine($"RunCommand: {command}");

					Console.WriteLine("AppSettings -> Backup: " + configHandler.Arguments.AppSettingsArguments.Backup);
					Console.WriteLine("Combined    -> Backup: " + configHandler.Arguments.CombinedArguments.Backup);
					Console.WriteLine("CommandLine -> Backup: " + configHandler.Arguments.CommandLineArguments.Backup);

					exitCode = ExecuteCommand(command, configHandler, serviceProvider);
				} catch(FileNotFoundException e) {
					exitCode = ExitCodes.FileNotFound;
					Console.WriteLine(e.Message);
				} catch(Exception e) {
					exitCode = ExitCodes.FatalError;
					Console.WriteException(e);
				} finally {
					stopwatch.Stop();
					Logger.ZLogTrace($"Stop application");
					globalLogger.ZLogInformation($"Application finished in {stopwatch.Elapsed} with exit code {exitCode}");
				}
			}
			return exitCode;
		}
		private static int ExecuteCommand(RunCommand command, ConfigurationHandler configHandler, IServiceProvider serviceProvider) {
			if(configHandler.Arguments.HasTest) {
				Console.WriteLine("Test er ikke implementeret endnu.");
				return ExitCodes.Success;
			}
			switch(command) {
				case RunCommand.UNKNOWN:
					Console.WriteLine("Der er sket et ukendt fejl.");
					return ExitCodes.UnknownCommand;
				case RunCommand.ERROR:
					Console.WriteLine("Der er sket en fejl.");
					return ExitCodes.GenericError;
				case RunCommand.HELP:
					Console.WriteLine("Du har kaldt hjælp.");
					return ExitCodes.Success;
				case RunCommand.BACKUP:
					Console.MarkupLine("Du har valgt [bold invert]BACKUP[/].");
					Console.WriteLine($"Med default settings : {configHandler.Arguments.CombinedArguments.DefaultConfigurationFile}");
					Console.WriteLine($"Med valgt settings   : {configHandler.Arguments.CombinedArguments.Backup}");
					BackupMaster backupMaster = serviceProvider.GetService<BackupMaster>()!;
					backupMaster.StartBackup(configHandler);
					return ExitCodes.Success;
				case RunCommand.VERIFY:
					Console.MarkupLine("Du har valgt [bold invert]VERIFY[/] en BACKUP.");
					Console.WriteLine($"Med default settings : {configHandler.Arguments.CombinedArguments.DefaultConfigurationFile}");
					Console.WriteLine($"Med valgt settings   : {configHandler.Arguments.CombinedArguments.Verify}");
					Console.WriteLine("Desværre lukker jeg nu da jeg ikke har implementeret kaldet endnu.");
					return ExitCodes.Success;
				case RunCommand.VERIFY_PATH:
					Console.MarkupLine("Du har valgt [bold invert]VERIFY_PATH[/] til en backup folder");
					Console.WriteLine($"Med verify path   : {configHandler.Arguments.CombinedArguments.VerifyPath}");
					Console.WriteLine("Desværre lukker jeg nu da jeg ikke har implementeret kaldet endnu men en lille er begyndt.");
					if(configHandler.VerifyBackups()) {
						Console.WriteLine("VerifyBackups");
						VerifyBackupHandler verifyBackupHandler = serviceProvider.GetService<VerifyBackupHandler>()!;
						verifyBackupHandler.VerifyBackup(configHandler.Arguments.CombinedArguments.VerifyPath!);
					}
					return ExitCodes.Success;
				default:
					Console.WriteLine("" + configHandler.Arguments);
					Console.WriteLine("Wrong line");
					return ExitCodes.GenericError;
			}
		}

		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
				.ConfigureServices((hostContext, services) => {
					// Opret en instans af StartUp-klassen
					var startup = new StartUp(hostContext.Configuration, new CancellationTokenSource());

					// Registrer services fra StartUp-klassen
					//foreach(var service in startup.ServiceProvider.GetServices<IServiceDescriptor>()) {
					//						services.Add(service);
					//}
				});
	}
	internal static class ExitCodes {
		public const int Success = 0;           // Normal completion
		public const int UnknownCommand = -10;  // Argument parsing produced unknown
		public const int GenericError = -1;     // Generic recoverable error
		public const int FileNotFound = -2;     // Required file missing
		public const int FatalError = -99;      // Unhandled exception
		public const int UnhandledError = -100; // Initialization or unexpected failure
	}
}