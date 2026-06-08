using System.CommandLine;
using System.CommandLine.Parsing;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using Core4BackupSourceType = BMTP3.Core4.Api.Models.Enums.BackupSourceType;
using Core4CollisionStrategy = BMTP3.Core4.Api.Models.Enums.CollisionStrategy;
using Core4CollisionComparisonType = BMTP3.Core4.Api.Models.Enums.CollisionComparisonType;
using Core4RenameStrategy = BMTP3.Core4.Api.Models.Enums.RenameStrategy;
using Core4SidecarFormat = BMTP3.Core4.Api.Models.Enums.SidecarFormat;
using Core4BackupIndexType = BMTP3.Core4.Api.Models.Enums.BackupIndexType;
using Core4OutputStructureStrategy = BMTP3.Core4.Api.Models.Enums.OutputStructureStrategy;
using Core4PostWriteVerificationType = BMTP3.Core4.Api.Models.Enums.PostWriteVerificationType;
using Core4HashAlgorithmType = BMTP3.Core4.Api.Models.Enums.HashAlgorithmType;

namespace BMTP3.Consoles.ConsoleCommands;

internal static class BackupConsoleCommand4Helpers
{
	public static BMTP3.Core4.Api.Models.BackupPlan BuildPlan(BackupOptionsModel backupOptions, ParseResult parseResult)
	{
		ArgumentNullException.ThrowIfNull(backupOptions);
		ArgumentNullException.ThrowIfNull(parseResult);

		static bool WasSupplied<T>(ParseResult pr, Option<T> option)
		{
			var res = pr.GetResult(option);
			return res != null && !res.Implicit;
		}

		string name = "backup";
		string sourcePath = string.Empty;
		string destination = string.Empty;
		Core4BackupSourceType sourceType = Core4BackupSourceType.FileSystem;
		bool recursive = true;
		bool dryRun = false;
		List<string>? includePatterns = null;
		List<string>? excludePatterns = null;
		Core4OutputStructureStrategy outputStrategy = Core4OutputStructureStrategy.PreserveHierarchy;
		string? customOutputPattern = null;
		Core4CollisionStrategy collisionStrategy = Core4CollisionStrategy.Rename;
		Core4CollisionComparisonType collisionComparison = Core4CollisionComparisonType.Binary;
		Core4RenameStrategy renameStrategy = Core4RenameStrategy.Increment;
		string? customCollisionPattern = null;
		Core4SidecarFormat sidecarFormat = Core4SidecarFormat.Ini;
		Core4BackupIndexType backupIndex = Core4BackupIndexType.None;
		Core4PostWriteVerificationType postWriteVerification = Core4PostWriteVerificationType.None;
		List<Core4HashAlgorithmType> comparisonHashAlgorithms = new() { Core4HashAlgorithmType.SHA2_256 };
		List<Core4HashAlgorithmType> verificationHashAlgorithms = new() { Core4HashAlgorithmType.SHA2_256 };

		if(WasSupplied(parseResult, BackupOptionsModel.NameOption) && !string.IsNullOrWhiteSpace(backupOptions.Name))
			name = backupOptions.Name;

		if(WasSupplied(parseResult, BackupOptionsModel.SourceDeviceOption) && !string.IsNullOrWhiteSpace(backupOptions.SourceDevice))
		{
			sourceType = Core4BackupSourceType.MediaDevice;
			sourcePath = backupOptions.SourceDevice;
		}

		if(WasSupplied(parseResult, BackupOptionsModel.SourceDirectoryOption) && !string.IsNullOrWhiteSpace(backupOptions.SourceDirectory))
		{
			sourceType = Core4BackupSourceType.FileSystem;
			sourcePath = backupOptions.SourceDirectory;
		}

		if(WasSupplied(parseResult, BackupOptionsModel.OutputDirectoryOption) && backupOptions.OutputDirectory != null)
			destination = backupOptions.OutputDirectory.FullName;

		if(WasSupplied(parseResult, BackupOptionsModel.RecursiveOption))
			recursive = backupOptions.Recursive;

		if(WasSupplied(parseResult, BackupOptionsModel.SimulateOption))
			dryRun = backupOptions.Simulate;

		if(WasSupplied(parseResult, BackupOptionsModel.IncludePatternsOption) && backupOptions.IncludePatterns?.Count > 0)
			includePatterns = backupOptions.IncludePatterns;

		if(WasSupplied(parseResult, BackupOptionsModel.ExcludePatternsOption) && backupOptions.ExcludePatterns?.Count > 0)
			excludePatterns = backupOptions.ExcludePatterns;

		if(WasSupplied(parseResult, BackupOptionsModel.OutputStrategyOption))
			outputStrategy = MapOutputStrategy(backupOptions.OutputStrategy);

		if(WasSupplied(parseResult, BackupOptionsModel.CustomOutputFilePathOption))
			customOutputPattern = backupOptions.CustomOutputFilePath;

		if(WasSupplied(parseResult, BackupOptionsModel.CollisionResolutionTypeOption))
			collisionStrategy = MapCollisionStrategy(backupOptions.CollisionResolutionType);

		if(WasSupplied(parseResult, BackupOptionsModel.CollisionComparisonOption))
			collisionComparison = MapCollisionComparison(backupOptions.CollisionComparison);

		if(WasSupplied(parseResult, BackupOptionsModel.RenameStrategyOption))
			renameStrategy = MapRenameStrategy(backupOptions.RenameStrategy);

		if(WasSupplied(parseResult, BackupOptionsModel.CustomCollisionOutputFilePathOption))
			customCollisionPattern = backupOptions.CustomCollisionOutputFilePath;

		if(WasSupplied(parseResult, BackupOptionsModel.SidecarFormatOption))
			sidecarFormat = MapSidecarFormat(backupOptions.SidecarFormat);

		if(WasSupplied(parseResult, BackupOptionsModel.BackupIndexTypeOption))
			backupIndex = MapBackupIndexType(backupOptions.BackupIndexType);

		if(WasSupplied(parseResult, BackupOptionsModel.PostWriteVerificationOption))
			postWriteVerification = MapPostWriteVerification(backupOptions.PostWriteVerification);

		// Normalize filesystem source path
		if(sourceType == Core4BackupSourceType.FileSystem && !string.IsNullOrWhiteSpace(sourcePath))
		{
			sourcePath = Path.GetFullPath(sourcePath);
		}

		// Default name from source or destination
		if(string.IsNullOrWhiteSpace(name) || name == "backup")
		{
			if(!string.IsNullOrWhiteSpace(backupOptions.Name))
				name = backupOptions.Name;
			else if(!string.IsNullOrWhiteSpace(sourcePath))
				name = Path.GetFileName(sourcePath.TrimEnd('/', '\\'));
			else if(!string.IsNullOrWhiteSpace(destination))
				name = Path.GetFileName(destination.TrimEnd('/', '\\'));
		}

		return new BMTP3.Core4.Api.Models.BackupPlan
		{
			Name = name,
			SourceType = sourceType,
			SourcePath = sourcePath,
			Recursive = recursive,
			IncludePatterns = includePatterns,
			ExcludePatterns = excludePatterns,
			Destination = destination,
			OutputStructureStrategy = outputStrategy,
			CustomOutputPattern = customOutputPattern,
			CollisionStrategy = collisionStrategy,
			CollisionComparisonType = collisionComparison,
			RenameStrategy = renameStrategy,
			CustomOutputCollisionPattern = customCollisionPattern,
			SidecarFormat = sidecarFormat,
			BackupIndexType = backupIndex,
			ComparisonHashAlgorithmTypes = comparisonHashAlgorithms,
			VerificationHashAlgorithmTypes = verificationHashAlgorithms,
			PostWriteVerification = postWriteVerification,
			EnableTimestampCorrection = true,
			DryRun = dryRun,
			ResumeBehavior = BMTP3.Core4.Api.Models.Enums.SessionResumeStrategy.Continue,
		};
	}

	private static Core4OutputStructureStrategy MapOutputStrategy(OutputStructureStrategy strategy)
	{
		return strategy switch
		{
			OutputStructureStrategy.PreserveSourceTree => Core4OutputStructureStrategy.PreserveHierarchy,
			OutputStructureStrategy.Flat => Core4OutputStructureStrategy.Flat,
			OutputStructureStrategy.CustomPathPattern => Core4OutputStructureStrategy.CustomPathPattern,
			_ => Core4OutputStructureStrategy.PreserveHierarchy,
		};
	}

	private static Core4CollisionStrategy MapCollisionStrategy(CollisionResolutionType strategy)
	{
		return strategy switch
		{
			CollisionResolutionType.Rename => Core4CollisionStrategy.Rename,
			CollisionResolutionType.Skip => Core4CollisionStrategy.Skip,
			CollisionResolutionType.Overwrite => Core4CollisionStrategy.Overwrite,
			CollisionResolutionType.Error => Core4CollisionStrategy.Error,
			_ => Core4CollisionStrategy.Rename,
		};
	}

	private static Core4CollisionComparisonType MapCollisionComparison(CollisionComparisonType type)
	{
		return type switch
		{
			CollisionComparisonType.None => Core4CollisionComparisonType.None,
			CollisionComparisonType.Hash => Core4CollisionComparisonType.Hash,
			CollisionComparisonType.Binary => Core4CollisionComparisonType.Binary,
			_ => Core4CollisionComparisonType.Binary,
		};
	}

	private static Core4RenameStrategy MapRenameStrategy(RenameStrategy strategy)
	{
		return strategy switch
		{
			RenameStrategy.Increment => Core4RenameStrategy.Increment,
			RenameStrategy.Timestamp => Core4RenameStrategy.Timestamp,
			RenameStrategy.Hash => Core4RenameStrategy.Hash,
			RenameStrategy.CustomCollisionPathPattern => Core4RenameStrategy.Custom,
			_ => Core4RenameStrategy.Increment,
		};
	}

	private static Core4SidecarFormat MapSidecarFormat(SidecarFormat format)
	{
		return format switch
		{
			SidecarFormat.None => Core4SidecarFormat.None,
			SidecarFormat.Ini => Core4SidecarFormat.Ini,
			SidecarFormat.Json => Core4SidecarFormat.Json,
			_ => Core4SidecarFormat.Ini,
		};
	}

	private static Core4BackupIndexType MapBackupIndexType(BackupIndexType type)
	{
		return type switch
		{
			BackupIndexType.None => Core4BackupIndexType.None,
			BackupIndexType.Json => Core4BackupIndexType.Json,
			BackupIndexType.Database => Core4BackupIndexType.Database,
			_ => Core4BackupIndexType.None,
		};
	}

	private static Core4PostWriteVerificationType MapPostWriteVerification(Core2.BackupNew.Api.Request.Enums.PostWriteVerificationType type)
	{
		return type switch
		{
			Core2.BackupNew.Api.Request.Enums.PostWriteVerificationType.None => Core4PostWriteVerificationType.None,
			Core2.BackupNew.Api.Request.Enums.PostWriteVerificationType.Hash => Core4PostWriteVerificationType.Hash,
			Core2.BackupNew.Api.Request.Enums.PostWriteVerificationType.Binary => Core4PostWriteVerificationType.Hash,
			_ => Core4PostWriteVerificationType.None,
		};
	}
}
