using BMTP3.Core.Configs;
using BMTP3.Core.Handlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core {
	[SupportedOSPlatform("windows10.0")]
	internal class AppHost {
		private readonly IAnsiConsole _console;
		private readonly IConfiguration _configuration;
		private readonly BackupSettingsReader _backupSettingsReader;
		private readonly IServiceProvider _serviceProvider;

		public AppHost(IAnsiConsole console, IConfiguration configuration, BackupSettingsReader backupSettingsReader, IServiceProvider serviceProvider) {
			_console = console;
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
				_console.MarkupLine($"Config File brugt: [green]{configHandler.BackupSettings?.BackupConfigFile}[/]");

				RunCommand command = configHandler.Arguments.GetRunCommand();
				_console.WriteLine($"Du har kørt RunCommand: {command}");

				_console.WriteLine("AppSettings -> Backup: " + configHandler.Arguments.AppSettingsArguments.Backup);
				_console.WriteLine("Combined    -> Backup: " + configHandler.Arguments.CombinedArguments.Backup);
				_console.WriteLine("CommandLine -> Backup: " + configHandler.Arguments.CommandLineArguments.Backup);

				return Task.FromResult(ExecuteCommand(command, configHandler));
			} catch(FileNotFoundException e) {
				_console.WriteLine(e.Message);
				_console.WriteLine("Exiting program");
			} catch(Exception e) {
				_console.WriteException(e);
			}

			return Task.FromResult(0);
		}

		private int ExecuteCommand(RunCommand command, ConfigurationHandler configHandler) {
			if(configHandler.Arguments.HasTest) {
				_console.WriteLine("Test er ikke implementeret endnu.");
				return 0;
			}
			switch(command) {
				case RunCommand.UNKNOWN:
					_console.WriteLine("Der er sket et ukendt fejl.");
					return -10;
				case RunCommand.ERROR:
					_console.WriteLine("Der er sket en fejl.");
					return -1;
				case RunCommand.HELP:
					_console.WriteLine("Du har kaldt hjælp.");
					return 0;
				case RunCommand.BACKUP:
					_console.MarkupLine("Du har valgt [bold invert]BACKUP[/].");
					_console.WriteLine($"Med default settings : {configHandler.Arguments.CombinedArguments.DefaultConfigurationFile}");
					_console.WriteLine($"Med valgt settings   : {configHandler.Arguments.CombinedArguments.Backup}");
					BackupMaster backupMaster = _serviceProvider.GetService<BackupMaster>()!;
					backupMaster.StartBackup(configHandler);
					return 0;
				case RunCommand.VERIFY:
					_console.MarkupLine("Du har valgt [bold invert]VERIFY[/] en BACKUP.");
					_console.WriteLine($"Med default settings : {configHandler.Arguments.CombinedArguments.DefaultConfigurationFile}");
					_console.WriteLine($"Med valgt settings   : {configHandler.Arguments.CombinedArguments.Verify}");
					_console.WriteLine("Desværre lukker jeg nu da jeg ikke har implementeret kaldet endnu.");
					return 0;
				case RunCommand.VERIFY_PATH:
					_console.MarkupLine("Du har valgt [bold invert]VERIFY_PATH[/] til en backup folder");
					_console.WriteLine($"Med verify path   : {configHandler.Arguments.CombinedArguments.VerifyPath}");
					_console.WriteLine("Desværre lukker jeg nu da jeg ikke har implementeret kaldet endnu men en lille er begyndt.");
					if(configHandler.VerifyBackups()) {
						_console.WriteLine("VerifyBackups");
						VerifyBackupHandler verifyBackupHandler = _serviceProvider.GetService<VerifyBackupHandler>()!;
						verifyBackupHandler.VerifyBackup(configHandler.Arguments.CombinedArguments.VerifyPath!);
						return 0;
					}
					return 0;
				default:
					_console.WriteLine("" + configHandler.Arguments);
					_console.WriteLine("Wrong line");
					return -1;
			}
		}
	}
}
