using BMTP3.Core.Configs;
using BMTP3.Core.Consoles;
using BMTP3.Core.Handlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using System.Runtime.Versioning;

namespace BMTP3.Core {
	[SupportedOSPlatform("windows10.0")]
	internal class AppHost {
		private readonly IAnsiConsole Console;
		private readonly IConfiguration _configuration;
		private readonly BackupSettingsReader _backupSettingsReader;
		private readonly IServiceProvider _serviceProvider;

		public AppHost(IAnsiConsole console, IConfiguration configuration, BackupSettingsReader backupSettingsReader, IServiceProvider serviceProvider) {
			Console = console;
			_configuration = configuration;
			_backupSettingsReader = backupSettingsReader;
			_serviceProvider = serviceProvider;
		}

		public Task<int> RunAsync(string[] args) {
			try {
				ConfigurationHandler configHandler = new ConfigurationHandler(
					args,
					_configuration,
					_backupSettingsReader
				);
				Console.MarkupLine($"Config File brugt: [green]{configHandler.BackupSettings?.BackupConfigFile}[/]");

				RunCommand command = configHandler.Arguments.GetRunCommand();
				Console.WriteLine($"Du har kørt RunCommand: {command}");

				Console.WriteLine("AppSettings -> Backup: " + configHandler.Arguments.AppSettingsArguments.Backup);
				Console.WriteLine("Combined    -> Backup: " + configHandler.Arguments.CombinedArguments.Backup);
				Console.WriteLine("CommandLine -> Backup: " + configHandler.Arguments.CommandLineArguments.Backup);

				return Task.FromResult(ExecuteCommand(command, configHandler, _serviceProvider));
			} catch(FileNotFoundException e) {
				Console.WriteLine(e.Message);
				Console.WriteLine("Exiting program");
			} catch(Exception e) {
				Console.WriteException(e);
			}

			return Task.FromResult(0);
		}

		private int ExecuteCommand(RunCommand command, ConfigurationHandler configHandler, IServiceProvider serviceProvider) {
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
	}
}
