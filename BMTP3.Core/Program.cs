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
using System.Runtime.Versioning;
using System.Text;
using ZLogger;
using SConsole = System.Console;

namespace BMTP3.Core {
	[SupportedOSPlatform("windows7.0")]
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
			SConsole.OutputEncoding = Encoding.UTF8;

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
			foreach(var x in args) {
				Console.WriteLine(x);
			}
			//args = ["--verify", "iPhone.toml"];
			args = ["--backup", "iPhone.toml"];
			//args = ["--verifyPath", ];


			await Task.Delay(1);

			//LogManager.Initialize(); // Force static initialize.

			globalLogger.ZLogCritical($"Application is starting.");

			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();

			Logger.ZLogTrace($"Start application");
			Console.WriteLine("Start application");
			using(cts) {
				ConsoleEventHandler.Initialize(cts);

				try {
					ConfigurationHandler configHandler = new ConfigurationHandler(
						args,
						configuration,
						serviceProvider.GetService<BackupSettingsReader>()!
					);
					Console.MarkupLine($"Config File brugt: [green]{configHandler.BackupSettings?.BackupConfigFile}[/]");

					RunCommand command = configHandler.Arguments.GetRunCommand();
					Console.WriteLine($"Du har lavet RunCommand: {command}");

					Console.WriteLine("AppSettings -> Backup: " + configHandler.Arguments.AppSettingsArguments.Backup);
					Console.WriteLine("Combined    -> Backup: " + configHandler.Arguments.CombinedArguments.Backup);
					Console.WriteLine("CommandLine -> Backup: " + configHandler.Arguments.CommandLineArguments.Backup);

					return ExecuteCommand(command, configHandler, serviceProvider);
				} catch(FileNotFoundException e) {
					Console.WriteLine(e.Message);
					Console.WriteLine("Exiting program");
				} catch(Exception e) {
					Console.WriteException(e);
				}
			}
			return 0;
		}
		private static int ExecuteCommand(RunCommand command, ConfigurationHandler configHandler, IServiceProvider serviceProvider) {
			if(configHandler.Arguments.HasTest) {
				Console.WriteLine("Test er ikke implementeret endnu.");
				return 0;
			}
			switch(command) {
				case RunCommand.UNKNOWN:
					Console.WriteLine("Der er sket et ukendt fejl.");
					return -10;
				case RunCommand.ERROR:
					Console.WriteLine("Der er sket en fejl.");
					return -1;
				case RunCommand.HELP:
					Console.WriteLine("Du har kaldt hjælp.");
					return 0;
				case RunCommand.BACKUP:
					Console.MarkupLine("Du har valgt [bold invert]BACKUP[/].");
					Console.WriteLine($"Med default settings : {configHandler.Arguments.CombinedArguments.DefaultConfigurationFile}");
					Console.WriteLine($"Med valgt settings   : {configHandler.Arguments.CombinedArguments.Backup}");
					BackupMaster backupMaster = serviceProvider.GetService<BackupMaster>()!;
					backupMaster.StartBackup(configHandler);
					return 0;
				case RunCommand.VERIFY:
					Console.MarkupLine("Du har valgt [bold invert]VERIFY[/] en BACKUP.");
					Console.WriteLine($"Med default settings : {configHandler.Arguments.CombinedArguments.DefaultConfigurationFile}");
					Console.WriteLine($"Med valgt settings   : {configHandler.Arguments.CombinedArguments.Verify}");
					Console.WriteLine("Desværre lukker jeg nu da jeg ikke har implementeret kaldet endnu.");
					return 0;
				case RunCommand.VERIFY_PATH:
					Console.MarkupLine("Du har valgt [bold invert]VERIFY_PATH[/] til en backup folder");
					Console.WriteLine($"Med verify path   : {configHandler.Arguments.CombinedArguments.VerifyPath}");
					Console.WriteLine("Desværre lukker jeg nu da jeg ikke har implementeret kaldet endnu men en lille er begyndt.");
					if(configHandler.VerifyBackups()) {
						Console.WriteLine("VerifyBackups");
						VerifyBackupHandler verifyBackupHandler = serviceProvider.GetService<VerifyBackupHandler>()!;
						verifyBackupHandler.VerifyBackup(configHandler.Arguments.CombinedArguments.VerifyPath!);
						return 0;
					}
					return 0;
				default:
					Console.WriteLine("" + configHandler.Arguments);
					Console.WriteLine("Wrong line");
					return -1;
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
}
