using System.CommandLine;
using System.CommandLine.Parsing;
using System.Reflection;
using BMTP3.Consoles.ConsoleCommands;
using BMTP3.Consoles.ConsoleCommands.Core4;
using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Api.Models.Enums;

namespace BMTP3.Consoles.Tests;

public class BackupPlan4BuildPlanTests
{
	private static readonly BackupConsoleCommand4 TestCommand = CreateTestCommand();

	private static BackupConsoleCommand4 CreateTestCommand()
	{
		return new BackupConsoleCommand4 { ServiceProvider = null! };
	}

	private static (BackupOptionsModel4, ParseResult) Parse(string cli)
	{
		ParseResult parseResult = TestCommand.Parse(cli);
		BackupOptionsModel4 options = new();
		options.ApplyOptions(parseResult);
		return (options, parseResult);
	}

	// ----------------------------------------------------------------
	// Config file only
	// ----------------------------------------------------------------

	[Fact]
	public void BuildPlan_WithTomlConfig_MapsAllSections()
	{
		string toml = @"
name = ""MyTest""
[source]
path = ""C:\\ConfigSource""
recursive = false
include-patterns = [""*.jpg""]
exclude-patterns = [""*.tmp""]
[destination]
path = ""D:\\ConfigDest""
output-structure = ""flat""
[collision]
strategy = ""skip""
comparison = ""hash""
rename-strategy = ""timestamp""
[metadata]
sidecar-format = ""json""
index-type = ""none""
enable-metadata = false
post-write-verification = ""hash""
enable-timestamp-correction = false
comparison-hash-algorithms = [""sha256""]
verification-hash-algorithms = [""sha512""]
[behavior]
dry-run = true
stop-on-error = false
delay = 500
resume-behavior = ""restart""
[execution]
max-degree-of-parallelism = 8
";
		string configPath = Path.GetTempFileName() + ".toml";
		try
		{
			File.WriteAllText(configPath, toml);
			var (options, parseResult) = Parse($"--config \"{configPath}\"");

			BackupPlan plan = BackupConsoleCommand4Helpers.BuildPlan(options, parseResult);

			Assert.Equal("MyTest", plan.Name);
			Assert.Equal(@"C:\ConfigSource", plan.SourcePath);
			Assert.False(plan.Recursive);
			Assert.Equal(["*.jpg"], plan.IncludePatterns);
			Assert.Equal(["*.tmp"], plan.ExcludePatterns);
			Assert.Equal(@"D:\ConfigDest", plan.Destination);
			Assert.Equal(OutputStructureStrategy.Flat, plan.OutputStructureStrategy);
			Assert.Equal(CollisionStrategy.Skip, plan.CollisionStrategy);
			Assert.Equal(CollisionComparisonType.Hash, plan.CollisionComparisonType);
			Assert.Equal(RenameStrategy.Timestamp, plan.RenameStrategy);
			Assert.Equal(SidecarFormat.Json, plan.SidecarFormat);
			Assert.False(plan.EnableMetadata);
			Assert.Equal(PostWriteVerificationType.Hash, plan.PostWriteVerification);
			Assert.False(plan.EnableTimestampCorrection);
			Assert.True(plan.DryRun);
			Assert.False(plan.StopOnError);
			Assert.Equal(500, plan.Delay);
			Assert.Equal(SessionResumeStrategy.Restart, plan.ResumeBehavior);
		}
		finally
		{
			File.Delete(configPath);
		}
	}

	[Fact]
	public void BuildPlan_WithJsonConfig_MapsAllSections()
	{
		string json = @"
{
  ""name"": ""JsonTest"",
  ""source"": {
    ""path"": ""C:\\JsonSource"",
    ""recursive"": true
  },
  ""destination"": {
    ""path"": ""D:\\JsonDest""
  },
  ""collision"": {
    ""strategy"": ""overwrite"",
    ""comparison"": ""binary""
  },
  ""metadata"": {
    ""sidecarFormat"": ""ini"",
    ""enableMetadata"": true
  },
  ""behavior"": {
    ""dryRun"": false
  },
  ""execution"": {
    ""maxDegreeOfParallelism"": 4
  }
}
";
		string configPath = Path.GetTempFileName() + ".json";
		try
		{
			File.WriteAllText(configPath, json);
			var (options, parseResult) = Parse($"--config \"{configPath}\"");

			BackupPlan plan = BackupConsoleCommand4Helpers.BuildPlan(options, parseResult);

			Assert.Equal("JsonTest", plan.Name);
			Assert.Equal(@"C:\JsonSource", plan.SourcePath);
			Assert.True(plan.Recursive);
			Assert.Equal(@"D:\JsonDest", plan.Destination);
			Assert.Equal(CollisionStrategy.Overwrite, plan.CollisionStrategy);
			Assert.Equal(CollisionComparisonType.Binary, plan.CollisionComparisonType);
			Assert.Equal(SidecarFormat.Ini, plan.SidecarFormat);
			Assert.True(plan.EnableMetadata);
			Assert.False(plan.DryRun);
		}
		finally
		{
			File.Delete(configPath);
		}
	}

	[Fact]
	public void BuildPlan_WithJson5Config_MapsAllSections()
	{
		string json5 = @"
{
  name: 'Json5Test',
  source: {
    path: 'C:\\Json5Source',
    recursive: true,
  },
  destination: {
    path: 'D:\\Json5Dest',
    outputStructure: 'preserve-hierarchy',
  },
  collision: {
    strategy: 'rename',
    comparison: 'size-and-modified-time',
    renameStrategy: 'increment',
  },
  metadata: {
    sidecarFormat: 'json',
    indexType: 'none',
    enableMetadata: false,
    enableTimestampCorrection: true,
  },
  behavior: {
    delay: 250,
  },
  execution: {
    maxDegreeOfParallelism: 2,
  },
}
";
		string configPath = Path.GetTempFileName() + ".json5";
		try
		{
			File.WriteAllText(configPath, json5);
			var (options, parseResult) = Parse($"--config \"{configPath}\"");

			BackupPlan plan = BackupConsoleCommand4Helpers.BuildPlan(options, parseResult);

			Assert.Equal("Json5Test", plan.Name);
			Assert.Equal(@"C:\Json5Source", plan.SourcePath);
			Assert.True(plan.Recursive);
			Assert.Equal(@"D:\Json5Dest", plan.Destination);
			Assert.Equal(CollisionStrategy.Rename, plan.CollisionStrategy);
			Assert.Equal(CollisionComparisonType.SizeAndModifiedTime, plan.CollisionComparisonType);
			Assert.Equal(RenameStrategy.Increment, plan.RenameStrategy);
			Assert.Equal(SidecarFormat.Json, plan.SidecarFormat);
			Assert.False(plan.EnableMetadata);
			Assert.True(plan.EnableTimestampCorrection);
			Assert.Equal(250, plan.Delay);
		}
		finally
		{
			File.Delete(configPath);
		}
	}

	// ----------------------------------------------------------------
	// Config + CLI override
	// ----------------------------------------------------------------

	[Fact]
	public void BuildPlan_WithConfig_CliOverridesSourcePath()
	{
		string toml = @"
name = ""OverrideTest""
[source]
path = ""C:\\FromConfig""
[destination]
path = ""D:\\FromConfig""
";
		string configPath = Path.GetTempFileName() + ".toml";
		try
		{
			File.WriteAllText(configPath, toml);
			var (options, parseResult) = Parse($"--config \"{configPath}\" --source-path \"C:\\FromCli\" --output \"D:\\FromCli\"");

			BackupPlan plan = BackupConsoleCommand4Helpers.BuildPlan(options, parseResult);

			Assert.Equal(@"C:\FromCli", plan.SourcePath);
			Assert.Equal(@"D:\FromCli", plan.Destination);
			Assert.Equal("OverrideTest", plan.Name);
		}
		finally
		{
			File.Delete(configPath);
		}
	}

	[Fact]
	public void BuildPlan_WithConfig_CliOverridesCollisionStrategy()
	{
		string toml = @"
[source]
path = ""C:\\Src""
[destination]
path = ""D:\\Dst""
[collision]
strategy = ""skip""
";
		string configPath = Path.GetTempFileName() + ".toml";
		try
		{
			File.WriteAllText(configPath, toml);
			var (options, parseResult) = Parse($"--config \"{configPath}\" --collision-strategy Overwrite");

			BackupPlan plan = BackupConsoleCommand4Helpers.BuildPlan(options, parseResult);

			Assert.Equal(CollisionStrategy.Overwrite, plan.CollisionStrategy);
		}
		finally
		{
			File.Delete(configPath);
		}
	}

	[Fact]
	public void BuildPlan_WithConfig_CliOverridesMultiple()
	{
		string toml = @"
name = ""Original""
[source]
path = ""C:\\Src""
[destination]
path = ""D:\\Dst""
[metadata]
sidecar-format = ""ini""
[behavior]
dry-run = true
";
		string configPath = Path.GetTempFileName() + ".toml";
		try
		{
			File.WriteAllText(configPath, toml);
			var (options, parseResult) = Parse(
				$"--config \"{configPath}\" " +
				$"--name \"Overridden\" " +
				$"--sidecar-format Json " +
				$"--dry-run false");

			BackupPlan plan = BackupConsoleCommand4Helpers.BuildPlan(options, parseResult);

			Assert.Equal("Overridden", plan.Name);
			Assert.Equal(SidecarFormat.Json, plan.SidecarFormat);
			Assert.False(plan.DryRun);
			Assert.Equal(CollisionStrategy.Rename, plan.CollisionStrategy);
			Assert.Equal(@"C:\Src", plan.SourcePath);
		}
		finally
		{
			File.Delete(configPath);
		}
	}

	// ----------------------------------------------------------------
	// CLI only (no config)
	// ----------------------------------------------------------------

	[Fact]
	public void BuildPlan_NoConfig_CliOnly()
	{
		string sourcePath = @"C:\CliSource";
		string destPath = @"D:\CliDest";
		var (options, parseResult) = Parse($"--source-path \"{sourcePath}\" --output \"{destPath}\"");

		BackupPlan plan = BackupConsoleCommand4Helpers.BuildPlan(options, parseResult);

		Assert.Equal(sourcePath, plan.SourcePath);
		Assert.Equal(destPath, plan.Destination);
		Assert.True(plan.Recursive);
		Assert.Equal(CollisionStrategy.Rename, plan.CollisionStrategy);
		Assert.Equal(CollisionComparisonType.Binary, plan.CollisionComparisonType);
		Assert.Equal(RenameStrategy.Increment, plan.RenameStrategy);
		Assert.Equal(SidecarFormat.Ini, plan.SidecarFormat);
		Assert.Equal(BackupIndexType.None, plan.BackupIndexType);
		Assert.False(plan.DryRun);
		Assert.True(plan.StopOnError);
		Assert.Equal(0, plan.Delay);
		Assert.False(plan.EnableMetadata);
		Assert.True(plan.EnableTimestampCorrection);
		Assert.Equal(SessionResumeStrategy.Continue, plan.ResumeBehavior);
	}

	[Fact]
	public void BuildPlan_NoConfig_ExplicitCliArgs()
	{
		var (options, parseResult) = Parse(
			$"--source-path \"C:\\Src\" " +
			$"--output \"D:\\Dst\" " +
			$"--recursive false " +
			$"--collision-strategy Skip " +
			$"--collision-compare None " +
			$"--sidecar-format None " +
			$"--dry-run true " +
			$"--delay 0 " +
			$"--verify Hash");

		BackupPlan plan = BackupConsoleCommand4Helpers.BuildPlan(options, parseResult);

		Assert.False(plan.Recursive);
		Assert.Equal(CollisionStrategy.Skip, plan.CollisionStrategy);
		Assert.Equal(CollisionComparisonType.None, plan.CollisionComparisonType);
		Assert.Equal(SidecarFormat.None, plan.SidecarFormat);
		Assert.True(plan.DryRun);
		Assert.Equal(0, plan.Delay);
		Assert.Equal(PostWriteVerificationType.Hash, plan.PostWriteVerification);
	}

	// ----------------------------------------------------------------
	// Config file not found
	// ----------------------------------------------------------------

	[Fact]
	public void BuildPlan_ConfigFileNotFound_UsesCliValues()
	{
		string missingPath = @"Z:\nonexistent\missing.toml";
		var (options, parseResult) = Parse(
			$"--config \"{missingPath}\" " +
			$"--source-path \"C:\\Fallback\" " +
			$"--output \"D:\\Fallback\"");

		BackupPlan plan = BackupConsoleCommand4Helpers.BuildPlan(options, parseResult);

		Assert.Equal(@"C:\Fallback", plan.SourcePath);
		Assert.Equal(@"D:\Fallback", plan.Destination);
	}

	// ----------------------------------------------------------------
	// Default config (--config without value)
	// ----------------------------------------------------------------

	[Fact]
	public void BuildPlan_WithConfigFlagOnly_UsesDefaultToml()
	{
		string cwd = Directory.GetCurrentDirectory();
		string defaultTomlPath = Path.Combine(cwd, "default.toml");
		try
		{
			File.WriteAllText(defaultTomlPath, @"
name = ""DefaultTest""
[source]
path = ""C:\\DefaultSrc""
[destination]
path = ""D:\\DefaultDst""
");
			var (options, parseResult) = Parse("--config");

			BackupPlan plan = BackupConsoleCommand4Helpers.BuildPlan(options, parseResult);

			Assert.Equal("DefaultTest", plan.Name);
			Assert.Equal(@"C:\DefaultSrc", plan.SourcePath);
			Assert.Equal(@"D:\DefaultDst", plan.Destination);
		}
		finally
		{
			if (File.Exists(defaultTomlPath))
				File.Delete(defaultTomlPath);
		}
	}

	[Fact]
	public void BuildPlan_WithConfigFlagOnly_UsesDefaultJson()
	{
		string cwd = Directory.GetCurrentDirectory();
		string defaultJsonPath = Path.Combine(cwd, "default.json");
		try
		{
			File.WriteAllText(defaultJsonPath, @"
{
  ""name"": ""DefaultJsonTest"",
  ""source"": {
    ""path"": ""C:\\JsonSrc""
  },
  ""destination"": {
    ""path"": ""D:\\JsonDst""
  }
}
");
			var (options, parseResult) = Parse("--config");

			BackupPlan plan = BackupConsoleCommand4Helpers.BuildPlan(options, parseResult);

			Assert.Equal("DefaultJsonTest", plan.Name);
			Assert.Equal(@"C:\JsonSrc", plan.SourcePath);
			Assert.Equal(@"D:\JsonDst", plan.Destination);
		}
		finally
		{
			if (File.Exists(defaultJsonPath))
				File.Delete(defaultJsonPath);
		}
	}

	[Fact]
	public void BuildPlan_ConfigFlagWithoutDefault_UsesCliValues()
	{
		string cwd = Directory.GetCurrentDirectory();
		string defaultTomlPath = Path.Combine(cwd, "default.toml");
		try
		{
			if (File.Exists(defaultTomlPath))
				File.Delete(defaultTomlPath);
			string defaultJsonPath = Path.Combine(cwd, "default.json");
			if (File.Exists(defaultJsonPath))
				File.Delete(defaultJsonPath);
			string defaultJson5Path = Path.Combine(cwd, "default.json5");
			if (File.Exists(defaultJson5Path))
				File.Delete(defaultJson5Path);

			var (options, parseResult) = Parse("--config --source-path \"C:\\NoDefault\" --output \"D:\\NoDefault\"");

			BackupPlan plan = BackupConsoleCommand4Helpers.BuildPlan(options, parseResult);

			Assert.Equal(@"C:\NoDefault", plan.SourcePath);
			Assert.Equal(@"D:\NoDefault", plan.Destination);
		}
		finally
		{
			if (File.Exists(defaultTomlPath))
				File.Delete(defaultTomlPath);
		}
	}

	// ----------------------------------------------------------------
	// Config with partial values (minimal config)
	// ----------------------------------------------------------------

	[Fact]
	public void BuildPlan_WithConfig_MinimalSourceAndDestination()
	{
		string toml = @"
name = ""Minimal""
[source]
path = ""C:\\MinSrc""
[destination]
path = ""D:\\MinDst""
";
		string configPath = Path.GetTempFileName() + ".toml";
		try
		{
			File.WriteAllText(configPath, toml);
			var (options, parseResult) = Parse($"--config \"{configPath}\"");

			BackupPlan plan = BackupConsoleCommand4Helpers.BuildPlan(options, parseResult);

			Assert.Equal("Minimal", plan.Name);
			Assert.Equal(@"C:\MinSrc", plan.SourcePath);
			Assert.Equal(@"D:\MinDst", plan.Destination);
			Assert.True(plan.Recursive);
			Assert.Null(plan.IncludePatterns);
			Assert.Null(plan.ExcludePatterns);
			Assert.Equal(OutputStructureStrategy.PreserveHierarchy, plan.OutputStructureStrategy);
			Assert.Equal(CollisionStrategy.Rename, plan.CollisionStrategy);
			Assert.Equal(CollisionComparisonType.Binary, plan.CollisionComparisonType);
			Assert.False(plan.DryRun);
			Assert.True(plan.StopOnError);
		}
		finally
		{
			File.Delete(configPath);
		}
	}

	// ----------------------------------------------------------------
	// Hash algorithm aliases (via config)
	// ----------------------------------------------------------------

	[Fact]
	public void BuildPlan_WithConfig_HashAlgorithmAliases_MergeToUnique()
	{
		string toml = @"
[source]
path = ""C:\\Src""
[destination]
path = ""D:\\Dst""
[metadata]
comparison-hash-algorithms = [""sha256"", ""sha2-256"", ""sha2_256"", ""SHA256""]
verification-hash-algorithms = [""md5"", ""md5-128"", ""md5_128""]
";
		string configPath = Path.GetTempFileName() + ".toml";
		try
		{
			File.WriteAllText(configPath, toml);
			var (options, parseResult) = Parse($"--config \"{configPath}\"");

			BackupPlan plan = BackupConsoleCommand4Helpers.BuildPlan(options, parseResult);

			Assert.Single(plan.ComparisonHashAlgorithmTypes);
			Assert.Equal(HashAlgorithmType.SHA2_256, plan.ComparisonHashAlgorithmTypes[0]);
			Assert.Single(plan.VerificationHashAlgorithmTypes);
			Assert.Equal(HashAlgorithmType.MD5_128, plan.VerificationHashAlgorithmTypes[0]);
		}
		finally
		{
			File.Delete(configPath);
		}
	}

	[Fact]
	public void BuildPlan_WithConfig_AllHashAlgorithmTypes()
	{
		string toml = @"
[source]
path = ""C:\\Src""
[destination]
path = ""D:\\Dst""
[metadata]
comparison-hash-algorithms = [""sha256"", ""sha512"", ""md5"", ""blake3-256"", ""blake3-512"", ""sha3-256"", ""sha3-512""]
";
		string configPath = Path.GetTempFileName() + ".toml";
		try
		{
			File.WriteAllText(configPath, toml);
			var (options, parseResult) = Parse($"--config \"{configPath}\"");

			BackupPlan plan = BackupConsoleCommand4Helpers.BuildPlan(options, parseResult);

			Assert.Equal(7, plan.ComparisonHashAlgorithmTypes.Count);
			Assert.Contains(HashAlgorithmType.SHA2_256, plan.ComparisonHashAlgorithmTypes);
			Assert.Contains(HashAlgorithmType.SHA2_512, plan.ComparisonHashAlgorithmTypes);
			Assert.Contains(HashAlgorithmType.MD5_128, plan.ComparisonHashAlgorithmTypes);
			Assert.Contains(HashAlgorithmType.BLAKE3_256, plan.ComparisonHashAlgorithmTypes);
			Assert.Contains(HashAlgorithmType.BLAKE3_512, plan.ComparisonHashAlgorithmTypes);
			Assert.Contains(HashAlgorithmType.SHA3_256_FIPS202, plan.ComparisonHashAlgorithmTypes);
			Assert.Contains(HashAlgorithmType.SHA3_512_FIPS202, plan.ComparisonHashAlgorithmTypes);
		}
		finally
		{
			File.Delete(configPath);
		}
	}

	[Fact]
	public void BuildPlan_WithConfig_InvalidHashAlgorithm_Throws()
	{
		string toml = @"
[source]
path = ""C:\\Src""
[destination]
path = ""D:\\Dst""
[metadata]
comparison-hash-algorithms = [""not-a-real-algorithm""]
";
		string configPath = Path.GetTempFileName() + ".toml";
		try
		{
			File.WriteAllText(configPath, toml);
			var (options, parseResult) = Parse($"--config \"{configPath}\"");

			Assert.Throws<ArgumentException>(() =>
				BackupConsoleCommand4Helpers.BuildPlan(options, parseResult));
		}
		finally
		{
			File.Delete(configPath);
		}
	}

	// ----------------------------------------------------------------
	// Invalid enum values (via config)
	// ----------------------------------------------------------------

	[Fact]
	public void BuildPlan_WithConfig_InvalidCollisionStrategy_Throws()
	{
		string toml = @"
[source]
path = ""C:\\Src""
[destination]
path = ""D:\\Dst""
[collision]
strategy = ""garbage-value""
";
		string configPath = Path.GetTempFileName() + ".toml";
		try
		{
			File.WriteAllText(configPath, toml);
			var (options, parseResult) = Parse($"--config \"{configPath}\"");

			Assert.Throws<ArgumentException>(() =>
				BackupConsoleCommand4Helpers.BuildPlan(options, parseResult));
		}
		finally
		{
			File.Delete(configPath);
		}
	}

	[Fact]
	public void BuildPlan_WithConfig_InvalidOutputStructure_Throws()
	{
		string toml = @"
[source]
path = ""C:\\Src""
[destination]
path = ""D:\\Dst""
[destination]
output-structure = ""bogus""
";
		string configPath = Path.GetTempFileName() + ".toml";
		try
		{
			File.WriteAllText(configPath, toml);
			var (options, parseResult) = Parse($"--config \"{configPath}\"");

			Assert.Throws<ArgumentException>(() =>
				BackupConsoleCommand4Helpers.BuildPlan(options, parseResult));
		}
		finally
		{
			File.Delete(configPath);
		}
	}

	// ----------------------------------------------------------------
	// CLI enum overrides
	// ----------------------------------------------------------------

	[Fact]
	public void BuildPlan_NoConfig_CliOverrides_AllEnumOptions()
	{
		var (options, parseResult) = Parse(
			$"--source-path \"C:\\Src\" " +
			$"--output \"D:\\Dst\" " +
			$"--output-structure Flat " +
			$"--collision-strategy Overwrite " +
			$"--collision-compare Hash " +
			$"--rename-strategy Timestamp " +
			$"--sidecar-format Json " +
			$"--verify Hash");

		BackupPlan plan = BackupConsoleCommand4Helpers.BuildPlan(options, parseResult);

		Assert.Equal(OutputStructureStrategy.Flat, plan.OutputStructureStrategy);
		Assert.Equal(CollisionStrategy.Overwrite, plan.CollisionStrategy);
		Assert.Equal(CollisionComparisonType.Hash, plan.CollisionComparisonType);
		Assert.Equal(RenameStrategy.Timestamp, plan.RenameStrategy);
		Assert.Equal(SidecarFormat.Json, plan.SidecarFormat);
		Assert.Equal(PostWriteVerificationType.Hash, plan.PostWriteVerification);
	}

	[Fact]
	public void BuildPlan_NoConfig_CliOverrides_CollisionCompareNone()
	{
		var (options, parseResult) = Parse(
			$"--source-path \"C:\\Src\" " +
			$"--output \"D:\\Dst\" " +
			$"--collision-compare None");

		BackupPlan plan = BackupConsoleCommand4Helpers.BuildPlan(options, parseResult);

		Assert.Equal(CollisionComparisonType.None, plan.CollisionComparisonType);
	}

	[Fact]
	public void BuildPlan_NoConfig_CliOverrides_RenameStrategy()
	{
		var (options, parseResult) = Parse(
			$"--source-path \"C:\\Src\" " +
			$"--output \"D:\\Dst\" " +
			$"--rename-strategy Timestamp");

		BackupPlan plan = BackupConsoleCommand4Helpers.BuildPlan(options, parseResult);

		Assert.Equal(RenameStrategy.Timestamp, plan.RenameStrategy);
	}

	// ----------------------------------------------------------------
	// MTP source detection
	// ----------------------------------------------------------------

	[Fact]
	public void BuildPlan_NoConfig_MtpSource_DetectsMediaDevice()
	{
		var (options, parseResult) = Parse("--source-path \"mtp://Apple iPad/Internal Storage/DCIM\"");

		BackupPlan plan = BackupConsoleCommand4Helpers.BuildPlan(options, parseResult);

		Assert.Equal(BackupSourceType.MediaDevice, plan.SourceType);
		Assert.Equal("mtp://Apple iPad/Internal Storage/DCIM", plan.SourcePath);
	}

	[Fact]
	public void BuildPlan_NoConfig_FileSystemSource_DefaultsFileSystem()
	{
		var (options, parseResult) = Parse("--source-path \"C:\\MyPictures\"");

		BackupPlan plan = BackupConsoleCommand4Helpers.BuildPlan(options, parseResult);

		Assert.Equal(BackupSourceType.FileSystem, plan.SourceType);
	}

	// ----------------------------------------------------------------
	// Config: rename strategies
	// ----------------------------------------------------------------

	[Fact]
	public void BuildPlan_WithConfig_AllRenameStrategies()
	{
		string[] strategies = ["increment", "timestamp", "hash", "custom-pattern"];
		RenameStrategy[] expected = [RenameStrategy.Increment, RenameStrategy.Timestamp, RenameStrategy.Hash, RenameStrategy.CustomPattern];

		for (int i = 0; i < strategies.Length; i++)
		{
			string toml = $@"
[source]
path = ""C:\\Src""
[destination]
path = ""D:\\Dst""
[collision]
strategy = ""rename""
rename-strategy = ""{strategies[i]}""
";
			string configPath = Path.GetTempFileName() + ".toml";
			try
			{
				File.WriteAllText(configPath, toml);
				var (options, parseResult) = Parse($"--config \"{configPath}\"");

				BackupPlan plan = BackupConsoleCommand4Helpers.BuildPlan(options, parseResult);

				Assert.Equal(expected[i], plan.RenameStrategy);
			}
			finally
			{
				File.Delete(configPath);
			}
		}
	}

	// ----------------------------------------------------------------
	// Config: enum case normalization
	// ----------------------------------------------------------------

	[Fact]
	public void BuildPlan_WithConfig_EnumCaseAndSeparatorNormalization()
	{
		string toml = @"
[source]
path = ""C:\\Src""
[destination]
path = ""D:\\Dst""
output-structure = ""PRESERVE-HIERARCHY""
[collision]
strategy = ""SKIP""
comparison = ""SIZE-AND-MODIFIED-TIME""
rename-strategy = ""TIMESTAMP""
[metadata]
sidecar-format = ""NONE""
post-write-verification = ""HASH""
[behavior]
resume-behavior = ""RESTART""
";
		string configPath = Path.GetTempFileName() + ".toml";
		try
		{
			File.WriteAllText(configPath, toml);
			var (options, parseResult) = Parse($"--config \"{configPath}\"");

			BackupPlan plan = BackupConsoleCommand4Helpers.BuildPlan(options, parseResult);

			Assert.Equal(OutputStructureStrategy.PreserveHierarchy, plan.OutputStructureStrategy);
			Assert.Equal(CollisionStrategy.Skip, plan.CollisionStrategy);
			Assert.Equal(CollisionComparisonType.SizeAndModifiedTime, plan.CollisionComparisonType);
			Assert.Equal(RenameStrategy.Timestamp, plan.RenameStrategy);
			Assert.Equal(SidecarFormat.None, plan.SidecarFormat);
			Assert.Equal(PostWriteVerificationType.Hash, plan.PostWriteVerification);
			Assert.Equal(SessionResumeStrategy.Restart, plan.ResumeBehavior);
		}
		finally
		{
			File.Delete(configPath);
		}
	}

	[Fact]
	public void BuildPlan_WithConfig_EnumUnderscoreNormalization()
	{
		string toml = @"
[source]
path = ""C:\\Src""
[destination]
path = ""D:\\Dst""
output-structure = ""preserve_hierarchy""
[collision]
strategy = ""rename""
comparison = ""size_and_modified_time""
[metadata]
sidecar-format = ""none""
";
		string configPath = Path.GetTempFileName() + ".toml";
		try
		{
			File.WriteAllText(configPath, toml);
			var (options, parseResult) = Parse($"--config \"{configPath}\"");

			BackupPlan plan = BackupConsoleCommand4Helpers.BuildPlan(options, parseResult);

			Assert.Equal(OutputStructureStrategy.PreserveHierarchy, plan.OutputStructureStrategy);
			Assert.Equal(CollisionComparisonType.SizeAndModifiedTime, plan.CollisionComparisonType);
		}
		finally
		{
			File.Delete(configPath);
		}
	}

	// ----------------------------------------------------------------
	// Hash algorithm CLI options
	// ----------------------------------------------------------------

	[Fact]
	public void BuildPlan_NoConfig_DefaultHash_AllTypes()
	{
		var (options, parseResult) = Parse("--source-path \"C:\\Src\" --output \"D:\\Dst\"");

		BackupPlan plan = BackupConsoleCommand4Helpers.BuildPlan(options, parseResult);

		Assert.Equal(Enum.GetValues<HashAlgorithmType>().Length, plan.ComparisonHashAlgorithmTypes.Count);
		Assert.Contains(HashAlgorithmType.SHA2_256, plan.ComparisonHashAlgorithmTypes);
		Assert.Contains(HashAlgorithmType.BLAKE3_512, plan.ComparisonHashAlgorithmTypes);
		Assert.Contains(HashAlgorithmType.SHA3_512_FIPS202, plan.ComparisonHashAlgorithmTypes);
		Assert.Equal(Enum.GetValues<HashAlgorithmType>().Length, plan.VerificationHashAlgorithmTypes.Count);
		Assert.Contains(HashAlgorithmType.SHA2_256, plan.VerificationHashAlgorithmTypes);
		Assert.Contains(HashAlgorithmType.BLAKE3_512, plan.VerificationHashAlgorithmTypes);
	}

	[Fact]
	public void BuildPlan_NoConfig_CliComparisonHash_Single()
	{
		var (options, parseResult) = Parse(
			$"--source-path \"C:\\Src\" --output \"D:\\Dst\" --comparison-hash sha256");

		BackupPlan plan = BackupConsoleCommand4Helpers.BuildPlan(options, parseResult);

		Assert.Single(plan.ComparisonHashAlgorithmTypes);
		Assert.Equal(HashAlgorithmType.SHA2_256, plan.ComparisonHashAlgorithmTypes[0]);
	}

	[Fact]
	public void BuildPlan_NoConfig_CliComparisonHash_Multiple()
	{
		var (options, parseResult) = Parse(
			$"--source-path \"C:\\Src\" --output \"D:\\Dst\" --comparison-hash sha256 --comparison-hash blake3-256");

		BackupPlan plan = BackupConsoleCommand4Helpers.BuildPlan(options, parseResult);

		Assert.Equal(2, plan.ComparisonHashAlgorithmTypes.Count);
		Assert.Contains(HashAlgorithmType.SHA2_256, plan.ComparisonHashAlgorithmTypes);
		Assert.Contains(HashAlgorithmType.BLAKE3_256, plan.ComparisonHashAlgorithmTypes);
	}

	[Fact]
	public void BuildPlan_NoConfig_CliVerificationHash_Single()
	{
		var (options, parseResult) = Parse(
			$"--source-path \"C:\\Src\" --output \"D:\\Dst\" --verification-hash sha512");

		BackupPlan plan = BackupConsoleCommand4Helpers.BuildPlan(options, parseResult);

		Assert.Single(plan.VerificationHashAlgorithmTypes);
		Assert.Equal(HashAlgorithmType.SHA2_512, plan.VerificationHashAlgorithmTypes[0]);
	}

	[Fact]
	public void BuildPlan_NoConfig_CliVerificationHash_Multiple()
	{
		var (options, parseResult) = Parse(
			$"--source-path \"C:\\Src\" --output \"D:\\Dst\" --verification-hash md5 --verification-hash sha3-256");

		BackupPlan plan = BackupConsoleCommand4Helpers.BuildPlan(options, parseResult);

		Assert.Equal(2, plan.VerificationHashAlgorithmTypes.Count);
		Assert.Contains(HashAlgorithmType.MD5_128, plan.VerificationHashAlgorithmTypes);
		Assert.Contains(HashAlgorithmType.SHA3_256_FIPS202, plan.VerificationHashAlgorithmTypes);
	}

	[Fact]
	public void BuildPlan_NoConfig_CliComparisonHash_Invalid_Throws()
	{
		var (options, parseResult) = Parse(
			$"--source-path \"C:\\Src\" --output \"D:\\Dst\" --comparison-hash garbage");

		Assert.Throws<ArgumentException>(() =>
			BackupConsoleCommand4Helpers.BuildPlan(options, parseResult));
	}

	[Fact]
	public void BuildPlan_NoConfig_CliVerificationHash_Invalid_Throws()
	{
		var (options, parseResult) = Parse(
			$"--source-path \"C:\\Src\" --output \"D:\\Dst\" --verification-hash garbage");

		Assert.Throws<ArgumentException>(() =>
			BackupConsoleCommand4Helpers.BuildPlan(options, parseResult));
	}

	[Fact]
	public void BuildPlan_WithConfig_CliHashOverridesConfig()
	{
		string toml = @"
[source]
path = ""C:\\Src""
[destination]
path = ""D:\\Dst""
[metadata]
comparison-hash-algorithms = [""md5""]
verification-hash-algorithms = [""sha512""]
";
		string configPath = Path.GetTempFileName() + ".toml";
		try
		{
			File.WriteAllText(configPath, toml);
			var (options, parseResult) = Parse(
				$"--config \"{configPath}\" --comparison-hash sha256 --verification-hash blake3-512");

			BackupPlan plan = BackupConsoleCommand4Helpers.BuildPlan(options, parseResult);

			Assert.Single(plan.ComparisonHashAlgorithmTypes);
			Assert.Equal(HashAlgorithmType.SHA2_256, plan.ComparisonHashAlgorithmTypes[0]);
			Assert.Single(plan.VerificationHashAlgorithmTypes);
			Assert.Equal(HashAlgorithmType.BLAKE3_512, plan.VerificationHashAlgorithmTypes[0]);
		}
		finally
		{
			File.Delete(configPath);
		}
	}
}
