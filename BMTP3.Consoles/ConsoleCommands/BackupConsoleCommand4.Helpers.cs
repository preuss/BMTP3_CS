using System.CommandLine;
using BMTP3.Consoles.Configs;
using BMTP3.Consoles.ConsoleCommands.Core4;
using BMTP3.Core4.Api.Models;

namespace BMTP3.Consoles.ConsoleCommands;

internal static class BackupConsoleCommand4Helpers
{
	public static BackupPlan BuildPlan(BackupOptionsModel4 backupOptions, ParseResult parseResult)
	{
		ArgumentNullException.ThrowIfNull(backupOptions);
		ArgumentNullException.ThrowIfNull(parseResult);

		BackupPlanBuilder builder = BackupPlanBuilder.CreateDefault();
		FileInfo? configFile = backupOptions.Config;

		if (configFile == null && OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.ConfigOption))
			configFile = BackupPlan4Loader.FindDefaultConfig();

		if (configFile?.Exists == true)
			builder.ApplyConfig(BackupPlan4Loader.Load(configFile));

		builder.ApplyCliOverrides(backupOptions, parseResult);

		return builder.ToBackupPlan();
	}
}