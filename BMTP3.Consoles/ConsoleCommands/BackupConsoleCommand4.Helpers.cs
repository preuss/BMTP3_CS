using BMTP3.Consoles.Configs;
using BMTP3.Consoles.ConsoleCommands.Core4;
using BMTP3.Core4.Api.Models.Enums;
using System.CommandLine;
using System.CommandLine.Parsing;

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
		BackupSourceType sourceType = BackupSourceType.FileSystem;
		bool recursive = true;
		bool dryRun = false;
		bool stopOnError = true;
		List<string>? includePatterns = null;
		List<string>? excludePatterns = null;
		OutputStructureStrategy outputStrategy = OutputStructureStrategy.PreserveHierarchy;
		string? customOutputPattern = null;
		CollisionStrategy collisionStrategy = CollisionStrategy.Rename;
		CollisionComparisonType collisionComparison = CollisionComparisonType.Binary;
		RenameStrategy renameStrategy = RenameStrategy.Increment;
		string? customCollisionPattern = null;
		SidecarFormat sidecarFormat = SidecarFormat.Ini;
		BackupIndexType backupIndex = BackupIndexType.None;
		PostWriteVerificationType postWriteVerification = PostWriteVerificationType.None;
		List<HashAlgorithmType> comparisonHashAlgorithms = new() { HashAlgorithmType.SHA2_256 };
		List<HashAlgorithmType> verificationHashAlgorithms = new() { HashAlgorithmType.SHA2_256 };
		int delay = 0;
		bool enableMetadata = false;
		bool enableTimestampCorrection = true;
		SessionResumeStrategy resumeBehavior = SessionResumeStrategy.Continue;
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
						sourceType = BackupSourceType.MediaDevice;
					else
						sourceType = BackupSourceType.FileSystem;
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
				sourceType = BackupSourceType.MediaDevice;
			}
			else
			{
				sourceType = BackupSourceType.FileSystem;
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
			outputStrategy = backupOptions.OutputStrategy;

		if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.CustomOutputFilePathOption))
			customOutputPattern = backupOptions.CustomOutputFilePath;

		if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.CollisionStrategyOption))
			collisionStrategy = backupOptions.CollisionStrategy;

		if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.CollisionComparisonOption))
			collisionComparison = backupOptions.CollisionComparison;

		if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.RenameStrategyOption))
			renameStrategy = backupOptions.RenameStrategy;

		if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.CustomCollisionOutputFilePathOption))
			customCollisionPattern = backupOptions.CustomCollisionOutputFilePath;

		if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.SidecarFormatOption))
			sidecarFormat = backupOptions.SidecarFormat;

		if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.BackupIndexTypeOption))
			backupIndex = backupOptions.BackupIndexType;

		if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.PostWriteVerificationOption))
			postWriteVerification = backupOptions.PostWriteVerification;

		// Normalize filesystem source path
		if (sourceType == BackupSourceType.FileSystem && !string.IsNullOrWhiteSpace(sourcePath))
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

	// ----------------------------------------------------------------
	// String-based parsers for config file values
	// ----------------------------------------------------------------

	private static OutputStructureStrategy ParseOutputStructure(string value)
	{
		return value.ToLowerInvariant() switch
		{
			"preserve-hierarchy" => OutputStructureStrategy.PreserveHierarchy,
			"flat" => OutputStructureStrategy.Flat,
			"custom-path-pattern" => OutputStructureStrategy.CustomPathPattern,
			_ => OutputStructureStrategy.PreserveHierarchy,
		};
	}

	private static CollisionStrategy ParseCollisionStrategy(string value)
	{
		return value.ToLowerInvariant() switch
		{
			"rename" => CollisionStrategy.Rename,
			"skip" => CollisionStrategy.Skip,
			"overwrite" => CollisionStrategy.Overwrite,
			"error" => CollisionStrategy.Error,
			_ => CollisionStrategy.Rename,
		};
	}

	private static CollisionComparisonType ParseCollisionComparison(string value)
	{
		return value.ToLowerInvariant() switch
		{
			"none" => CollisionComparisonType.None,
			"hash" => CollisionComparisonType.Hash,
			"binary" => CollisionComparisonType.Binary,
			"size-and-modified-time" => CollisionComparisonType.SizeAndModifiedTime,
			_ => CollisionComparisonType.Binary,
		};
	}

	private static RenameStrategy ParseRenameStrategy(string value)
	{
		return value.ToLowerInvariant() switch
		{
			"increment" => RenameStrategy.Increment,
			"timestamp" => RenameStrategy.Timestamp,
			"hash" => RenameStrategy.Hash,
			"custom-pattern" => RenameStrategy.CustomPattern,
			_ => RenameStrategy.Increment,
		};
	}

	private static SidecarFormat ParseSidecarFormat(string value)
	{
		return value.ToLowerInvariant() switch
		{
			"none" => SidecarFormat.None,
			"ini" => SidecarFormat.Ini,
			"json" => SidecarFormat.Json,
			_ => SidecarFormat.Ini,
		};
	}

	private static BackupIndexType ParseBackupIndexType(string value)
	{
		return value.ToLowerInvariant() switch
		{
			"none" => BackupIndexType.None,
			"json" => BackupIndexType.Json,
			"database" => BackupIndexType.Database,
			_ => BackupIndexType.None,
		};
	}

	private static List<HashAlgorithmType> ParseHashAlgorithms(List<string> values)
	{
		List<HashAlgorithmType> result = new(values.Count);
		foreach (string v in values)
		{
			HashAlgorithmType parsed = ParseHashAlgorithm(v);
			if (!result.Contains(parsed))
				result.Add(parsed);
		}
		return result;
	}

	private static HashAlgorithmType ParseHashAlgorithm(string value)
	{
		return value.Trim().ToLowerInvariant() switch
		{
			"sha256" or "sha2-256" or "sha2_256" => HashAlgorithmType.SHA2_256,
			"sha512" or "sha2-512" or "sha2_512" => HashAlgorithmType.SHA2_512,
			"sha3-256" or "sha3_256" => HashAlgorithmType.SHA3_256_FIPS202,
			"sha3-512" or "sha3_512" => HashAlgorithmType.SHA3_512_FIPS202,
			"md5" or "md5-128" or "md5_128" => HashAlgorithmType.MD5_128,
			"blake3" or "blake3-256" or "blake3_256" => HashAlgorithmType.BLAKE3_256,
			"blake3-512" or "blake3_512" => HashAlgorithmType.BLAKE3_512,
			_ => HashAlgorithmType.SHA2_256,
		};
	}

	private static PostWriteVerificationType ParsePostWriteVerification(string value)
	{
		return value.ToLowerInvariant() switch
		{
			"none" => PostWriteVerificationType.None,
			"hash" => PostWriteVerificationType.Hash,
			_ => PostWriteVerificationType.None,
		};
	}

	private static SessionResumeStrategy ParseResumeBehavior(string value)
	{
		return value.ToLowerInvariant() switch
		{
			"abort" => SessionResumeStrategy.Abort,
			"continue" => SessionResumeStrategy.Continue,
			"restart" => SessionResumeStrategy.Restart,
			_ => SessionResumeStrategy.Continue,
		};
	}
}
