using BMTP3.Consoles.Configs;
using BMTP3.Consoles.ConsoleCommands.Core4;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using System.CommandLine;
using System.CommandLine.Parsing;
using Core4BackupIndexType = BMTP3.Core4.Api.Models.Enums.BackupIndexType;
using Core4BackupSourceType = BMTP3.Core4.Api.Models.Enums.BackupSourceType;
using Core4CollisionComparisonType = BMTP3.Core4.Api.Models.Enums.CollisionComparisonType;
using Core4CollisionStrategy = BMTP3.Core4.Api.Models.Enums.CollisionStrategy;
using Core4HashAlgorithmType = BMTP3.Core4.Api.Models.Enums.HashAlgorithmType;
using Core4OutputStructureStrategy = BMTP3.Core4.Api.Models.Enums.OutputStructureStrategy;
using Core4PostWriteVerificationType = BMTP3.Core4.Api.Models.Enums.PostWriteVerificationType;
using Core4RenameStrategy = BMTP3.Core4.Api.Models.Enums.RenameStrategy;
using Core4SessionResumeStrategy = BMTP3.Core4.Api.Models.Enums.SessionResumeStrategy;
using Core4SidecarFormat = BMTP3.Core4.Api.Models.Enums.SidecarFormat;

namespace BMTP3.Consoles.ConsoleCommands;

internal static class BackupConsoleCommand4Helpers
{
	public static BMTP3.Core4.Api.Models.BackupPlan BuildPlan(BackupOptionsModel4 backupOptions, ParseResult parseResult)
	{
		ArgumentNullException.ThrowIfNull(backupOptions);
		ArgumentNullException.ThrowIfNull(parseResult);

		string name = "backup";
		string sourcePath = string.Empty;
		string destination = string.Empty;
		Core4BackupSourceType sourceType = Core4BackupSourceType.FileSystem;
		bool recursive = true;
		bool dryRun = false;
		bool stopOnError = true;
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
		int delay = 0;
		bool enableMetadata = false;
		bool enableTimestampCorrection = true;
		Core4SessionResumeStrategy resumeBehavior = Core4SessionResumeStrategy.Continue;
		int? maxDegreeOfParallelism = null;

		if (backupOptions.Config is { Exists: true })
		{
			BackupPlan4Config config = BackupPlan4Loader.Load(backupOptions.Config);

			if (!string.IsNullOrWhiteSpace(config.Name))
				name = config.Name;

			if (config.Source != null)
			{
				if (!string.IsNullOrWhiteSpace(config.Source.Path))
				{
					sourcePath = config.Source.Path;
					if (sourcePath.StartsWith("mtp://", StringComparison.Ordinal))
						sourceType = Core4BackupSourceType.MediaDevice;
					else
						sourceType = Core4BackupSourceType.FileSystem;
				}

				recursive = config.Source.Recursive;

				if (config.Source.IncludePatterns?.Count > 0)
					includePatterns = config.Source.IncludePatterns;

				if (config.Source.ExcludePatterns?.Count > 0)
					excludePatterns = config.Source.ExcludePatterns;
			}

			if (config.Destination != null)
			{
				if (!string.IsNullOrWhiteSpace(config.Destination.Path))
					destination = config.Destination.Path;

				if (!string.IsNullOrWhiteSpace(config.Destination.OutputStructure))
					outputStrategy = ParseOutputStructure(config.Destination.OutputStructure);

				if (!string.IsNullOrWhiteSpace(config.Destination.CustomOutputPattern))
					customOutputPattern = config.Destination.CustomOutputPattern;
			}

			if (config.Collision != null)
			{
				if (!string.IsNullOrWhiteSpace(config.Collision.Strategy))
					collisionStrategy = ParseCollisionStrategy(config.Collision.Strategy);

				if (!string.IsNullOrWhiteSpace(config.Collision.Comparison))
					collisionComparison = ParseCollisionComparison(config.Collision.Comparison);

				if (!string.IsNullOrWhiteSpace(config.Collision.RenameStrategy))
					renameStrategy = ParseRenameStrategy(config.Collision.RenameStrategy);

				if (!string.IsNullOrWhiteSpace(config.Collision.CustomPattern))
					customCollisionPattern = config.Collision.CustomPattern;
			}

			if (config.Metadata != null)
			{
				if (!string.IsNullOrWhiteSpace(config.Metadata.SidecarFormat))
					sidecarFormat = ParseSidecarFormat(config.Metadata.SidecarFormat);

				if (!string.IsNullOrWhiteSpace(config.Metadata.IndexType))
					backupIndex = ParseBackupIndexType(config.Metadata.IndexType);

				if (config.Metadata.ComparisonHashAlgorithms?.Count > 0)
					comparisonHashAlgorithms = ParseHashAlgorithms(config.Metadata.ComparisonHashAlgorithms);

				if (config.Metadata.VerificationHashAlgorithms?.Count > 0)
					verificationHashAlgorithms = ParseHashAlgorithms(config.Metadata.VerificationHashAlgorithms);

				enableMetadata = config.Metadata.EnableMetadata;

				if (!string.IsNullOrWhiteSpace(config.Metadata.PostWriteVerification))
					postWriteVerification = ParsePostWriteVerification(config.Metadata.PostWriteVerification);

				enableTimestampCorrection = config.Metadata.EnableTimestampCorrection;
			}

			if (config.Behavior != null)
			{
				dryRun = config.Behavior.DryRun;
				stopOnError = config.Behavior.StopOnError;
				delay = config.Behavior.Delay;

				if (!string.IsNullOrWhiteSpace(config.Behavior.ResumeBehavior))
					resumeBehavior = ParseResumeBehavior(config.Behavior.ResumeBehavior);
			}

			if (config.Execution != null)
			{
				maxDegreeOfParallelism = config.Execution.MaxDegreeOfParallelism;
			}
		}

		if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.NameOption) && !string.IsNullOrWhiteSpace(backupOptions.Name))
			name = backupOptions.Name;

		if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.SourcePathOption) && !string.IsNullOrWhiteSpace(backupOptions.SourcePath))
		{
			sourcePath = backupOptions.SourcePath;

			if (sourcePath.StartsWith("mtp://", StringComparison.Ordinal))
			{
				sourceType = Core4BackupSourceType.MediaDevice;
			}
			else
			{
				sourceType = Core4BackupSourceType.FileSystem;
				sourcePath = Path.GetFullPath(sourcePath);
			}
		}

		if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.OutputDirectoryOption) && backupOptions.OutputDirectory != null)
			destination = backupOptions.OutputDirectory.FullName;

		if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.RecursiveOption))
			recursive = backupOptions.Recursive;

		if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.SimulateOption))
			dryRun = backupOptions.Simulate;

		if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.IncludePatternsOption) && backupOptions.IncludePatterns?.Count > 0)
			includePatterns = backupOptions.IncludePatterns;

		if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.ExcludePatternsOption) && backupOptions.ExcludePatterns?.Count > 0)
			excludePatterns = backupOptions.ExcludePatterns;

		if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.OutputStrategyOption))
			outputStrategy = MapOutputStrategy(backupOptions.OutputStrategy);

		if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.CustomOutputFilePathOption))
			customOutputPattern = backupOptions.CustomOutputFilePath;

		if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.CollisionResolutionTypeOption))
			collisionStrategy = MapCollisionStrategy(backupOptions.CollisionResolutionType);

		if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.CollisionComparisonOption))
			collisionComparison = MapCollisionComparison(backupOptions.CollisionComparison);

		if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.RenameStrategyOption))
			renameStrategy = MapRenameStrategy(backupOptions.RenameStrategy);

		if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.CustomCollisionOutputFilePathOption))
			customCollisionPattern = backupOptions.CustomCollisionOutputFilePath;

		if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.SidecarFormatOption))
			sidecarFormat = MapSidecarFormat(backupOptions.SidecarFormat);

		if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.BackupIndexTypeOption))
			backupIndex = MapBackupIndexType(backupOptions.BackupIndexType);

		if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.PostWriteVerificationOption))
			postWriteVerification = MapPostWriteVerification(backupOptions.PostWriteVerification);

		// Normalize filesystem source path
		if (sourceType == Core4BackupSourceType.FileSystem && !string.IsNullOrWhiteSpace(sourcePath))
		{
			sourcePath = Path.GetFullPath(sourcePath);
		}

		// Default name from source or destination
		if (string.IsNullOrWhiteSpace(name) || name == "backup")
		{
			if (!string.IsNullOrWhiteSpace(backupOptions.Name))
				name = backupOptions.Name;
			else if (!string.IsNullOrWhiteSpace(sourcePath))
				name = Path.GetFileName(sourcePath.TrimEnd('/', '\\'));
			else if (!string.IsNullOrWhiteSpace(destination))
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
			EnableMetadata = enableMetadata,
			PostWriteVerification = postWriteVerification,
			EnableTimestampCorrection = enableTimestampCorrection,
			DryRun = dryRun,
			StopOnError = stopOnError,
			Delay = delay,
			ResumeBehavior = resumeBehavior,
			MaxDegreeOfParallelism = maxDegreeOfParallelism,
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

	// ----------------------------------------------------------------
	// String-based parsers for config file values
	// ----------------------------------------------------------------

	private static Core4OutputStructureStrategy ParseOutputStructure(string value)
	{
		return value.ToLowerInvariant() switch
		{
			"preserve-hierarchy" => Core4OutputStructureStrategy.PreserveHierarchy,
			"flat" => Core4OutputStructureStrategy.Flat,
			"custom-path-pattern" => Core4OutputStructureStrategy.CustomPathPattern,
			_ => Core4OutputStructureStrategy.PreserveHierarchy,
		};
	}

	private static Core4CollisionStrategy ParseCollisionStrategy(string value)
	{
		return value.ToLowerInvariant() switch
		{
			"rename" => Core4CollisionStrategy.Rename,
			"skip" => Core4CollisionStrategy.Skip,
			"overwrite" => Core4CollisionStrategy.Overwrite,
			"error" => Core4CollisionStrategy.Error,
			_ => Core4CollisionStrategy.Rename,
		};
	}

	private static Core4CollisionComparisonType ParseCollisionComparison(string value)
	{
		return value.ToLowerInvariant() switch
		{
			"none" => Core4CollisionComparisonType.None,
			"hash" => Core4CollisionComparisonType.Hash,
			"binary" => Core4CollisionComparisonType.Binary,
			"size-and-modified-time" => Core4CollisionComparisonType.SizeAndModifiedTime,
			_ => Core4CollisionComparisonType.Binary,
		};
	}

	private static Core4RenameStrategy ParseRenameStrategy(string value)
	{
		return value.ToLowerInvariant() switch
		{
			"increment" => Core4RenameStrategy.Increment,
			"append-number" => Core4RenameStrategy.Increment,
			"timestamp" => Core4RenameStrategy.Timestamp,
			"hash" => Core4RenameStrategy.Hash,
			"custom" => Core4RenameStrategy.Custom,
			_ => Core4RenameStrategy.Increment,
		};
	}

	private static Core4SidecarFormat ParseSidecarFormat(string value)
	{
		return value.ToLowerInvariant() switch
		{
			"none" => Core4SidecarFormat.None,
			"ini" => Core4SidecarFormat.Ini,
			"json" => Core4SidecarFormat.Json,
			_ => Core4SidecarFormat.Ini,
		};
	}

	private static Core4BackupIndexType ParseBackupIndexType(string value)
	{
		return value.ToLowerInvariant() switch
		{
			"none" => Core4BackupIndexType.None,
			"json" => Core4BackupIndexType.Json,
			"database" => Core4BackupIndexType.Database,
			_ => Core4BackupIndexType.None,
		};
	}

	private static List<Core4HashAlgorithmType> ParseHashAlgorithms(List<string> values)
	{
		List<Core4HashAlgorithmType> result = new(values.Count);
		foreach (string v in values)
		{
			Core4HashAlgorithmType parsed = ParseHashAlgorithm(v);
			if (!result.Contains(parsed))
				result.Add(parsed);
		}
		return result;
	}

	private static Core4HashAlgorithmType ParseHashAlgorithm(string value)
	{
		return value.Trim().ToLowerInvariant() switch
		{
			"sha256" or "sha2-256" or "sha2_256" => Core4HashAlgorithmType.SHA2_256,
			"sha512" or "sha2-512" or "sha2_512" => Core4HashAlgorithmType.SHA2_512,
			"sha3-256" or "sha3_256" => Core4HashAlgorithmType.SHA3_256_FIPS202,
			"sha3-512" or "sha3_512" => Core4HashAlgorithmType.SHA3_512_FIPS202,
			"md5" or "md5-128" or "md5_128" => Core4HashAlgorithmType.MD5_128,
			"blake3" or "blake3-256" or "blake3_256" => Core4HashAlgorithmType.BLAKE3_256,
			"blake3-512" or "blake3_512" => Core4HashAlgorithmType.BLAKE3_512,
			_ => Core4HashAlgorithmType.SHA2_256,
		};
	}

	private static Core4PostWriteVerificationType ParsePostWriteVerification(string value)
	{
		return value.ToLowerInvariant() switch
		{
			"none" => Core4PostWriteVerificationType.None,
			"hash" => Core4PostWriteVerificationType.Hash,
			_ => Core4PostWriteVerificationType.None,
		};
	}

	private static Core4SessionResumeStrategy ParseResumeBehavior(string value)
	{
		return value.ToLowerInvariant() switch
		{
			"abort" => Core4SessionResumeStrategy.Abort,
			"continue" => Core4SessionResumeStrategy.Continue,
			"restart" => Core4SessionResumeStrategy.Restart,
			_ => Core4SessionResumeStrategy.Continue,
		};
	}
}
