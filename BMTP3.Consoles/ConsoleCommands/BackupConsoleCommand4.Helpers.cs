using BMTP3.Consoles.Configs;
using BMTP3.Consoles.ConsoleCommands.Core4;
using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Api.Models.Enums;
using System.CommandLine;
using System.CommandLine.Parsing;

namespace BMTP3.Consoles.ConsoleCommands;

internal static class BackupConsoleCommand4Helpers
{
	public static BackupPlan BuildPlan(BackupOptionsModel4 backupOptions, ParseResult parseResult)
	{
		ArgumentNullException.ThrowIfNull(backupOptions);
		ArgumentNullException.ThrowIfNull(parseResult);

		BackupPlanBuilder builder = BackupPlanBuilder.CreateDefault();

		if (backupOptions.Config is { Exists: true })
			builder.ApplyConfig(BackupPlan4Loader.Load(backupOptions.Config));

		builder.ApplyCliOverrides(backupOptions, parseResult);

		return builder.ToBackupPlan();
	}

	private sealed class BackupPlanBuilder
	{
		public string Name = "backup";
		public string SourcePath = string.Empty;
		public string Destination = string.Empty;
		public BackupSourceType SourceType = BackupSourceType.FileSystem;
		public bool Recursive = true;
		public bool DryRun;
		public bool StopOnError = true;
		public List<string>? IncludePatterns;
		public List<string>? ExcludePatterns;
		public OutputStructureStrategy OutputStructureStrategy = OutputStructureStrategy.PreserveHierarchy;
		public string? CustomOutputPattern;
		public CollisionStrategy CollisionStrategy = CollisionStrategy.Rename;
		public CollisionComparisonType CollisionComparisonType = CollisionComparisonType.Binary;
		public RenameStrategy RenameStrategy = RenameStrategy.Increment;
		public string? CustomOutputCollisionPattern;
		public SidecarFormat SidecarFormat = SidecarFormat.Ini;
		public BackupIndexType BackupIndexType;
		public PostWriteVerificationType PostWriteVerification;
		public List<HashAlgorithmType> ComparisonHashAlgorithmTypes = new() { HashAlgorithmType.SHA2_256 };
		public List<HashAlgorithmType> VerificationHashAlgorithmTypes = new() { HashAlgorithmType.SHA2_256 };
		public bool EnableMetadata;
		public bool EnableTimestampCorrection = true;
		public int Delay;
		public SessionResumeStrategy ResumeBehavior = SessionResumeStrategy.Continue;

		public static BackupPlanBuilder CreateDefault() => new();

		public BackupPlan ToBackupPlan() => new()
		{
			Name = Name,
			SourceType = SourceType,
			SourcePath = SourcePath,
			Recursive = Recursive,
			IncludePatterns = IncludePatterns,
			ExcludePatterns = ExcludePatterns,
			Destination = Destination,
			OutputStructureStrategy = OutputStructureStrategy,
			CustomOutputPattern = CustomOutputPattern,
			CollisionStrategy = CollisionStrategy,
			CollisionComparisonType = CollisionComparisonType,
			RenameStrategy = RenameStrategy,
			CustomOutputCollisionPattern = CustomOutputCollisionPattern,
			SidecarFormat = SidecarFormat,
			BackupIndexType = BackupIndexType,
			ComparisonHashAlgorithmTypes = ComparisonHashAlgorithmTypes,
			VerificationHashAlgorithmTypes = VerificationHashAlgorithmTypes,
			EnableMetadata = EnableMetadata,
			PostWriteVerification = PostWriteVerification,
			EnableTimestampCorrection = EnableTimestampCorrection,
			DryRun = DryRun,
			StopOnError = StopOnError,
			Delay = Delay,
			ResumeBehavior = ResumeBehavior,
		};

		public void ApplyConfig(BackupPlan4Config config)
		{
			if (!string.IsNullOrWhiteSpace(config.Name))
				Name = config.Name;

			if (!string.IsNullOrWhiteSpace(config.Source.Path))
			{
				SourcePath = config.Source.Path;
				SourceType = config.Source.Path.StartsWith("mtp://", StringComparison.Ordinal)
					? BackupSourceType.MediaDevice
					: BackupSourceType.FileSystem;
			}

			Recursive = config.Source.Recursive;

			if (config.Source.IncludePatterns?.Count > 0)
				IncludePatterns = config.Source.IncludePatterns;

			if (config.Source.ExcludePatterns?.Count > 0)
				ExcludePatterns = config.Source.ExcludePatterns;

			if (!string.IsNullOrWhiteSpace(config.Destination.Path))
				Destination = config.Destination.Path;

			OutputStructureStrategy = ParseEnum<OutputStructureStrategy>(config.Destination.OutputStructure);

			if (!string.IsNullOrWhiteSpace(config.Destination.CustomOutputPattern))
				CustomOutputPattern = config.Destination.CustomOutputPattern;

			CollisionStrategy = ParseEnum<CollisionStrategy>(config.Collision.Strategy);
			CollisionComparisonType = ParseEnum<CollisionComparisonType>(config.Collision.Comparison);
			RenameStrategy = ParseEnum<RenameStrategy>(config.Collision.RenameStrategy);

			if (!string.IsNullOrWhiteSpace(config.Collision.CustomPattern))
				CustomOutputCollisionPattern = config.Collision.CustomPattern;

			SidecarFormat = ParseEnum<SidecarFormat>(config.Metadata.SidecarFormat);
			BackupIndexType = ParseEnum<BackupIndexType>(config.Metadata.IndexType);

			if (config.Metadata.ComparisonHashAlgorithms?.Count > 0)
				ComparisonHashAlgorithmTypes = ParseHashAlgorithms(config.Metadata.ComparisonHashAlgorithms);

			if (config.Metadata.VerificationHashAlgorithms?.Count > 0)
				VerificationHashAlgorithmTypes = ParseHashAlgorithms(config.Metadata.VerificationHashAlgorithms);

			EnableMetadata = config.Metadata.EnableMetadata;
			PostWriteVerification = ParseEnum<PostWriteVerificationType>(config.Metadata.PostWriteVerification);
			EnableTimestampCorrection = config.Metadata.EnableTimestampCorrection;

			DryRun = config.Behavior.DryRun;
			StopOnError = config.Behavior.StopOnError;
			Delay = config.Behavior.Delay;
			ResumeBehavior = ParseEnum<SessionResumeStrategy>(config.Behavior.ResumeBehavior);
		}

		public void ApplyCliOverrides(BackupOptionsModel4 backupOptions, ParseResult parseResult)
		{
			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.NameOption) && !string.IsNullOrWhiteSpace(backupOptions.Name))
				Name = backupOptions.Name;

			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.SourcePathOption) && !string.IsNullOrWhiteSpace(backupOptions.SourcePath))
			{
				SourcePath = backupOptions.SourcePath;
				if (SourcePath.StartsWith("mtp://", StringComparison.Ordinal))
					SourceType = BackupSourceType.MediaDevice;
				else
				{
					SourceType = BackupSourceType.FileSystem;
					SourcePath = Path.GetFullPath(SourcePath);
				}
			}

			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.OutputDirectoryOption) && backupOptions.OutputDirectory != null)
				Destination = backupOptions.OutputDirectory.FullName;

			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.RecursiveOption))
				Recursive = backupOptions.Recursive;

			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.SimulateOption))
				DryRun = backupOptions.Simulate;

			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.StopOnErrorOption))
				StopOnError = backupOptions.StopOnError;

			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.IncludePatternsOption) && backupOptions.IncludePatterns?.Count > 0)
				IncludePatterns = backupOptions.IncludePatterns;

			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.ExcludePatternsOption) && backupOptions.ExcludePatterns?.Count > 0)
				ExcludePatterns = backupOptions.ExcludePatterns;

			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.OutputStrategyOption))
				OutputStructureStrategy = backupOptions.OutputStrategy;

			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.CustomOutputFilePathOption))
				CustomOutputPattern = backupOptions.CustomOutputFilePath;

			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.CollisionStrategyOption))
				CollisionStrategy = backupOptions.CollisionStrategy;

			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.CollisionComparisonOption))
				CollisionComparisonType = backupOptions.CollisionComparison;

			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.RenameStrategyOption))
				RenameStrategy = backupOptions.RenameStrategy;

			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.CustomCollisionOutputFilePathOption))
				CustomOutputCollisionPattern = backupOptions.CustomCollisionOutputFilePath;

			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.SidecarFormatOption))
				SidecarFormat = backupOptions.SidecarFormat;

			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.BackupIndexTypeOption))
				BackupIndexType = backupOptions.BackupIndexType;

			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.PostWriteVerificationOption))
				PostWriteVerification = backupOptions.PostWriteVerification;

			if (OptionHelpers.WasSupplied(parseResult, BackupOptionsModel4.DelayOption))
				Delay = backupOptions.Delay;

			if (SourceType == BackupSourceType.FileSystem && !string.IsNullOrWhiteSpace(SourcePath))
				SourcePath = Path.GetFullPath(SourcePath);

			if (string.IsNullOrWhiteSpace(Name) || Name == "backup")
			{
				if (!string.IsNullOrWhiteSpace(backupOptions.Name))
					Name = backupOptions.Name;
				else if (!string.IsNullOrWhiteSpace(SourcePath))
					Name = Path.GetFileName(SourcePath.TrimEnd('/', '\\'));
				else if (!string.IsNullOrWhiteSpace(Destination))
					Name = Path.GetFileName(Destination.TrimEnd('/', '\\'));
			}
		}
	}

	// ----------------------------------------------------------------
	// Generic enum parser for config file string values
	// ----------------------------------------------------------------

	private static T ParseEnum<T>(string value) where T : struct, Enum
	{
		if (string.IsNullOrWhiteSpace(value))
			throw new ArgumentException($"Value cannot be null or empty for {typeof(T).Name}.");

		string normalized = value.Replace("-", "").Replace("_", "").Trim().ToLowerInvariant();

		foreach (T enumValue in Enum.GetValues<T>())
		{
			string enumName = enumValue.ToString()!.Replace("_", "").ToLowerInvariant();
			if (normalized == enumName)
				return enumValue;
		}

		throw new ArgumentException($"Invalid value '{value}' for {typeof(T).Name}. Valid values: {string.Join(", ", Enum.GetNames<T>())}");
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
			_ => throw new ArgumentException($"Invalid hash algorithm '{value}'."),
		};
	}
}
