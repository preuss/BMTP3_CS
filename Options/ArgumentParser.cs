using Fclp;
using Microsoft.Extensions.Configuration;
using OnixLabs.Core;
using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.Options {
	internal class ArgumentParser {
		public static CommandOptions GetArguments(string[] args, IConfigurationSection defaultArgsSettings) {
			//	args = ["-?", "--default", "default_asdfasdf.toml"];
			//	string defaultConfigFilePath = defaultOptions["config"];
			string? defaultConfigFilePath = defaultArgsSettings["default"] ?? default;
			string? backupConfigFilePath = defaultArgsSettings["backup"] ?? default;

			// Add Command Line Arguments here.
			ApplicationArguments appSettingsArguments = new ApplicationArguments();
			appSettingsArguments.DefaultConfigurationFile = defaultArgsSettings["default"];
			appSettingsArguments.Backup = defaultArgsSettings["backup"];
			appSettingsArguments.Verify = defaultArgsSettings["verify"];
			appSettingsArguments.VerifyPath = defaultArgsSettings["verifyPath"];

			FluentCommandLineParser<ApplicationArguments> options = new FluentCommandLineParser<ApplicationArguments>();

			options.Setup(arg => arg.DefaultConfigurationFile)
				.As('d', "default")
				.WithDescription($"The default config file, default value is [{defaultConfigFilePath}]")
				.SetDefault(defaultConfigFilePath);

			options.Setup(arg => arg.Backup)
				.As('b', "backup")
				.WithDescription("Backup command, you have to apply a backup config file. Example [iPhone.toml]");

			options.Setup(arg => arg.Verify)
				.As('v', "verify")
				.WithDescription("Verify a Backup using the backup config file. Example [iPhone.toml]");

			options.Setup(arg => arg.VerifyPath)
				.As('p', "verifyPath")
				.WithDescription("Verify a Backup using path to backup folder");

			options.Setup(arg => arg.Test)
				.As('t', "test")
				.SetDefault(false);

			options.SetupHelp("?", "help")
				.Callback(text => {
					text.Split('\n').ToList().ForEach(x => Console.WriteLine(x));
				}
			);

			ICommandLineParserResult parserResult = options.Parse(args);

			ApplicationArguments arguments = options.Object;


			if(parserResult.HasErrors) {
				options.HelpOption.ShowHelp(options.Options);
			} else if(parserResult.HelpCalled) {
			}

			if(string.IsNullOrEmpty(defaultConfigFilePath)) {
				throw new ArgumentException("Config File Path is empty.");
			}
			/*
			if(!File.Exists(arguments.BackupConfigurationFile)) {
				var isConfigUnmatched = parserResult.UnMatchedOptions.Any(opt => opt.LongName == "config" || opt.ShortName == "c");
				if(!isConfigUnmatched) {
					throw new FileNotFoundException($"Config File Path does not exist: {arguments.BackupConfigurationFile}");
				} else {
					throw new FileNotFoundException($"Using Default, Config File Path does not exist: {arguments.BackupConfigurationFile}");
				}
			}
			*/

			return new CommandOptions() {
				HelpCalled = parserResult.HelpCalled,
				HasErrors = parserResult.HasErrors,
				CommandLineArguments = arguments,
				AppSettingsArguments = appSettingsArguments
			};
		}
	}
}
