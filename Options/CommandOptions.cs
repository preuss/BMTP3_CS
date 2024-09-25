using BMTP3_CS.Configs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.Options {
	internal class CommandOptions {
		private IArguments? _combinedArguments;
		public required bool HelpCalled { get; init; }
		public required bool HasErrors { get; init; }
		public required IArguments CommandLineArguments { get; init; }
		public required IArguments AppSettingsArguments { get; init; }
		public IArguments CombinedArguments {
			get {
				if(_combinedArguments == null) {
					_combinedArguments = new ApplicationArguments() {
						Test = CommandLineArguments.Test || AppSettingsArguments.Test,
						DefaultConfigurationFile = CommandLineArguments.DefaultConfigurationFile ?? AppSettingsArguments.DefaultConfigurationFile,
						Backup = CommandLineArguments.Backup ?? AppSettingsArguments.Backup,
						Verify = CommandLineArguments.Verify ?? AppSettingsArguments.Verify,
						VerifyPath = CommandLineArguments.VerifyPath ?? AppSettingsArguments.VerifyPath,
					};
				}
				return _combinedArguments;
			}
		}
		public bool HasTest { get => CommandLineArguments.Test || AppSettingsArguments.Test; }

		public RunCommand GetRunCommand() {
			if(HasErrors) {
				return RunCommand.ERROR;
			}

			// Help is special --?
			if(HelpCalled) {
				return RunCommand.HELP;
			}

			// Using commandline arguments.
			if(!string.IsNullOrWhiteSpace(CommandLineArguments.Backup)) {
				return RunCommand.BACKUP;
			}
			if(!string.IsNullOrWhiteSpace(CommandLineArguments.Verify)) {
				return RunCommand.VERIFY;
			}
			if(!string.IsNullOrWhiteSpace(CommandLineArguments.VerifyPath)) {
				return RunCommand.VERIFY_PATH;
			}


			// Using combined after commandline arguments.
			if(CombinedArguments.Backup != null) {
				return RunCommand.BACKUP;
			}
			if(CombinedArguments.Verify != null) {
				return RunCommand.VERIFY;
			}
			if(CombinedArguments.VerifyPath != null) {
				return RunCommand.VERIFY_PATH;
			}
			return RunCommand.UNKNOWN;
		}

		public override string ToString() {
			return $"ApplicationConfiguration: \n" +
				   $"HasErrors: {HasErrors}, \n" +
				   $"Arguments: {CommandLineArguments.ToString()}, \n" +
				   $"AppSettingsArguments: {AppSettingsArguments.ToString()}, \n" +
				   $"RunCommand: {GetRunCommand()}";
		}
	}
}
