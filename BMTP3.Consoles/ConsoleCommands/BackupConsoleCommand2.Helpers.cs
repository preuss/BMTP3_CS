using BMTP3.Consoles.Configs;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using System.CommandLine;

namespace BMTP3.Consoles.ConsoleCommands
{
	internal static class BackupConsoleCommand2Helpers
	{
		public static BackupPlan BuildPlan(BackupOptionsModel backupOptions, ParseResult parseResult)
		{
			if (backupOptions == null) throw new ArgumentNullException(nameof(backupOptions));
			if (parseResult == null) throw new ArgumentNullException(nameof(parseResult));

			BackupPlan plan;
			if (backupOptions.Config != null && backupOptions.Config.Exists)
			{
				plan = BackupPlanLoader.Load(backupOptions.Config);
			}
			else
			{
				plan = new BackupPlan();
			}

			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel.NameOption) && !string.IsNullOrWhiteSpace(backupOptions.Name))
				plan.Name = backupOptions.Name;

			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel.SourceDeviceOption) && !string.IsNullOrWhiteSpace(backupOptions.SourceDevice))
			{
				plan.SourceType = SourceType.MediaDevice;
				plan.SourceId = backupOptions.SourceDevice;
			}

			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel.SourceDirectoryOption) && !string.IsNullOrWhiteSpace(backupOptions.SourceDirectory))
			{
				plan.SourceType = SourceType.FileSystem;
				plan.SourcePath = backupOptions.SourceDirectory;
			}

			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel.OutputDirectoryOption) && backupOptions.OutputDirectory != null)
				plan.OutputPath = backupOptions.OutputDirectory.FullName;

			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel.RecursiveOption))
				plan.Recursive = backupOptions.Recursive;

			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel.SimulateOption))
				plan.DryRun = backupOptions.Simulate;

			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel.IncludePatternsOption))
				plan.IncludePatterns = backupOptions.IncludePatterns;

			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel.ExcludePatternsOption))
				plan.ExcludePatterns = backupOptions.ExcludePatterns;

			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel.OutputStrategyOption))
				plan.OutputStrategy = backupOptions.OutputStrategy;

			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel.CustomOutputFilePathOption))
				plan.CustomOutputPathPattern = backupOptions.CustomOutputFilePath;

			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel.CollisionResolutionTypeOption))
				plan.CollisionResolution = backupOptions.CollisionResolutionType;

			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel.CollisionComparisonOption))
				plan.ComparisonType = backupOptions.CollisionComparison;

			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel.RenameStrategyOption))
				plan.RenameStrategy = backupOptions.RenameStrategy;

			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel.CustomCollisionOutputFilePathOption))
				plan.CustomCollisionPathPattern = backupOptions.CustomCollisionOutputFilePath;

			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel.SidecarFormatOption))
				plan.SidecarFormat = backupOptions.SidecarFormat;

			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel.BackupIndexTypeOption))
				plan.BackupIndexType = backupOptions.BackupIndexType;

			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel.DelayOption))
				plan.DelayMs = backupOptions.Delay;

			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel.PostWriteVerificationOption))
				plan.PostWriteVerification = backupOptions.PostWriteVerification;

			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel.VerificationRetryCountOption))
				plan.VerificationRetryCount = backupOptions.VerificationRetryCount;

			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel.VerificationRetryDelayMsOption))
				plan.VerificationRetryDelayMs = backupOptions.VerificationRetryDelayMs;

			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel.VerificationDeleteOnFailureOption))
				plan.VerificationDeleteOnFailure = backupOptions.VerificationDeleteOnFailure;

			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel.VerificationTimeoutMsOption))
				plan.VerificationTimeoutMs = backupOptions.VerificationTimeoutMs;

			// Ensure name fallback remains explicit and easy to reason about
			if (string.IsNullOrWhiteSpace(plan.Name))
			{
				if (!string.IsNullOrWhiteSpace(backupOptions.Name))
				{
					plan.Name = backupOptions.Name;
				}
				else if (backupOptions.Config != null)
				{
					plan.Name = Path.GetFileNameWithoutExtension(backupOptions.Config.Name);
				}
				else if (!string.IsNullOrWhiteSpace(backupOptions.SourceDevice))
				{
					plan.Name = backupOptions.SourceDevice;
				}
				else if (backupOptions.OutputDirectory != null)
				{
					plan.Name = backupOptions.OutputDirectory.Name;
				}
				else
				{
					plan.Name = "backup";
				}
			}

			// Compute SourceId and SourcePath normalization carefully:
			if (plan.SourceType == SourceType.FileSystem)
			{
				if (!string.IsNullOrWhiteSpace(plan.SourcePath))
				{
					plan.SourcePath = Path.GetFullPath(plan.SourcePath);
					plan.SourceId = Path.GetPathRoot(plan.SourcePath) ?? string.Empty;
				}
				else
				{
					plan.SourceId = Path.GetPathRoot(Path.GetFullPath(".")) ?? string.Empty;
				}
			}
			else if (plan.SourceType == SourceType.MediaDevice)
			{
				plan.SourceId ??= backupOptions.SourceDevice ?? string.Empty;
			}

			return plan;
		}
	}
}
