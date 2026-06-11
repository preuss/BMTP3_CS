using System.CommandLine;
using System.CommandLine.Parsing;
using System.Reflection;
using BMTP3.Consoles.ConsoleCommands;
using BMTP3.Consoles.ConsoleCommands.Core4;
using Core4Plan = BMTP3.Core4.Api.Models.BackupPlan;
using Core4CollisionStrategy = BMTP3.Core4.Api.Models.Enums.CollisionStrategy;
using Core4CollisionComparisonType = BMTP3.Core4.Api.Models.Enums.CollisionComparisonType;
using Core4RenameStrategy = BMTP3.Core4.Api.Models.Enums.RenameStrategy;
using Core4OutputStructureStrategy = BMTP3.Core4.Api.Models.Enums.OutputStructureStrategy;
using Core4SidecarFormat = BMTP3.Core4.Api.Models.Enums.SidecarFormat;
using Core4BackupIndexType = BMTP3.Core4.Api.Models.Enums.BackupIndexType;
using Core4PostWriteVerificationType = BMTP3.Core4.Api.Models.Enums.PostWriteVerificationType;
using Core4SessionResumeStrategy = BMTP3.Core4.Api.Models.Enums.SessionResumeStrategy;

namespace BMTP3.Consoles.Tests;

public class BackupPlan4BuildPlanTests
{
	private static readonly BackupConsoleCommand4 TestCommand = CreateTestCommand();

	private static BackupConsoleCommand4 CreateTestCommand()
	{
		return new BackupConsoleCommand4();
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

			Core4Plan plan = BackupConsoleCommand4Helpers.BuildPlan(options, parseResult);

			Assert.Equal("MyTest", plan.Name);
			Assert.Equal(@"C:\ConfigSource", plan.SourcePath);
			Assert.False(plan.Recursive);
			Assert.Equal(["*.jpg"], plan.IncludePatterns);
			Assert.Equal(["*.tmp"], plan.ExcludePatterns);
			Assert.Equal(@"D:\ConfigDest", plan.Destination);
			Assert.Equal(Core4OutputStructureStrategy.Flat, plan.OutputStructureStrategy);
			Assert.Equal(Core4CollisionStrategy.Skip, plan.CollisionStrategy);
			Assert.Equal(Core4CollisionComparisonType.Hash, plan.CollisionComparisonType);
			Assert.Equal(Core4RenameStrategy.Timestamp, plan.RenameStrategy);
			Assert.Equal(Core4SidecarFormat.Json, plan.SidecarFormat);
			Assert.False(plan.EnableMetadata);
			Assert.Equal(Core4PostWriteVerificationType.Hash, plan.PostWriteVerification);
			Assert.False(plan.EnableTimestampCorrection);
			Assert.True(plan.DryRun);
			Assert.False(plan.StopOnError);
			Assert.Equal(500, plan.Delay);
			Assert.Equal(Core4SessionResumeStrategy.Restart, plan.ResumeBehavior);
			Assert.Equal(8, plan.MaxDegreeOfParallelism);
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

			Core4Plan plan = BackupConsoleCommand4Helpers.BuildPlan(options, parseResult);

			Assert.Equal("JsonTest", plan.Name);
			Assert.Equal(@"C:\JsonSource", plan.SourcePath);
			Assert.True(plan.Recursive);
			Assert.Equal(@"D:\JsonDest", plan.Destination);
			Assert.Equal(Core4CollisionStrategy.Overwrite, plan.CollisionStrategy);
			Assert.Equal(Core4CollisionComparisonType.Binary, plan.CollisionComparisonType);
			Assert.Equal(Core4SidecarFormat.Ini, plan.SidecarFormat);
			Assert.True(plan.EnableMetadata);
			Assert.False(plan.DryRun);
			Assert.Equal(4, plan.MaxDegreeOfParallelism);
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
    renameStrategy: 'append-number',
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

			Core4Plan plan = BackupConsoleCommand4Helpers.BuildPlan(options, parseResult);

			Assert.Equal("Json5Test", plan.Name);
			Assert.Equal(@"C:\Json5Source", plan.SourcePath);
			Assert.True(plan.Recursive);
			Assert.Equal(@"D:\Json5Dest", plan.Destination);
			Assert.Equal(Core4CollisionStrategy.Rename, plan.CollisionStrategy);
			Assert.Equal(Core4CollisionComparisonType.SizeAndModifiedTime, plan.CollisionComparisonType);
			Assert.Equal(Core4RenameStrategy.Increment, plan.RenameStrategy);
			Assert.Equal(Core4SidecarFormat.Json, plan.SidecarFormat);
			Assert.False(plan.EnableMetadata);
			Assert.True(plan.EnableTimestampCorrection);
			Assert.Equal(250, plan.Delay);
			Assert.Equal(2, plan.MaxDegreeOfParallelism);
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

			Core4Plan plan = BackupConsoleCommand4Helpers.BuildPlan(options, parseResult);

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

			Core4Plan plan = BackupConsoleCommand4Helpers.BuildPlan(options, parseResult);

			Assert.Equal(Core4CollisionStrategy.Overwrite, plan.CollisionStrategy);
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

			Core4Plan plan = BackupConsoleCommand4Helpers.BuildPlan(options, parseResult);

			Assert.Equal("Overridden", plan.Name);
			Assert.Equal(Core4SidecarFormat.Json, plan.SidecarFormat);
			Assert.False(plan.DryRun);
			Assert.Equal(Core4CollisionStrategy.Rename, plan.CollisionStrategy);
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

		Core4Plan plan = BackupConsoleCommand4Helpers.BuildPlan(options, parseResult);

		Assert.Equal(sourcePath, plan.SourcePath);
		Assert.Equal(destPath, plan.Destination);
		Assert.True(plan.Recursive);
		Assert.Equal(Core4CollisionStrategy.Rename, plan.CollisionStrategy);
		Assert.Equal(Core4CollisionComparisonType.Binary, plan.CollisionComparisonType);
		Assert.Equal(Core4RenameStrategy.Increment, plan.RenameStrategy);
		Assert.Equal(Core4SidecarFormat.Ini, plan.SidecarFormat);
		Assert.Equal(Core4BackupIndexType.None, plan.BackupIndexType);
		Assert.False(plan.DryRun);
		Assert.True(plan.StopOnError);
		Assert.Equal(0, plan.Delay);
		Assert.False(plan.EnableMetadata);
		Assert.True(plan.EnableTimestampCorrection);
		Assert.Equal(Core4SessionResumeStrategy.Continue, plan.ResumeBehavior);
		Assert.Null(plan.MaxDegreeOfParallelism);
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

		Core4Plan plan = BackupConsoleCommand4Helpers.BuildPlan(options, parseResult);

		Assert.False(plan.Recursive);
		Assert.Equal(Core4CollisionStrategy.Skip, plan.CollisionStrategy);
		Assert.Equal(Core4CollisionComparisonType.None, plan.CollisionComparisonType);
		Assert.Equal(Core4SidecarFormat.None, plan.SidecarFormat);
		Assert.True(plan.DryRun);
		Assert.Equal(0, plan.Delay);
		Assert.Equal(Core4PostWriteVerificationType.Hash, plan.PostWriteVerification);
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

		Core4Plan plan = BackupConsoleCommand4Helpers.BuildPlan(options, parseResult);

		Assert.Equal(@"C:\Fallback", plan.SourcePath);
		Assert.Equal(@"D:\Fallback", plan.Destination);
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

			Core4Plan plan = BackupConsoleCommand4Helpers.BuildPlan(options, parseResult);

			Assert.Equal("Minimal", plan.Name);
			Assert.Equal(@"C:\MinSrc", plan.SourcePath);
			Assert.Equal(@"D:\MinDst", plan.Destination);
			Assert.True(plan.Recursive);
			Assert.Null(plan.IncludePatterns);
			Assert.Null(plan.ExcludePatterns);
			Assert.Equal(Core4OutputStructureStrategy.PreserveHierarchy, plan.OutputStructureStrategy);
			Assert.Equal(Core4CollisionStrategy.Rename, plan.CollisionStrategy);
			Assert.Equal(Core4CollisionComparisonType.Binary, plan.CollisionComparisonType);
			Assert.False(plan.DryRun);
			Assert.True(plan.StopOnError);
		}
		finally
		{
			File.Delete(configPath);
		}
	}
}
