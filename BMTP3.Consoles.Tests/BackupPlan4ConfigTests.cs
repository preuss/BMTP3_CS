using BMTP3.Consoles.Configs;

namespace BMTP3.Consoles.Tests;

public class BackupPlan4ConfigTests
{
	private const string TestDataDir = "TestData";

	// ----------------------------------------------------------------
	// BackupPlan4Loader — load TOML file
	// ----------------------------------------------------------------

	[Fact]
	public void Load_TomlFile_ParsesAllSections()
	{
		string tomlPath = Path.Combine(TestDataDir, "backup_config_test.toml");
		Assert.True(File.Exists(tomlPath), $"Test TOML file not found: {tomlPath}");

		BackupPlan4Config config = BackupPlan4Loader.Load(new FileInfo(tomlPath));

		Assert.NotNull(config);
		Assert.Equal("Pictures backup", config.Name);

		Assert.NotNull(config.Source);
		Assert.Equal("filesystem", config.Source.Type);
		Assert.Equal(@"C:\Users\John\Pictures", config.Source.Path);
		Assert.True(config.Source.Recursive);
		Assert.Equal(["*.jpg", "*.jpeg", "*.png"], config.Source.IncludePatterns);
		Assert.Equal(["*.tmp"], config.Source.ExcludePatterns);

		Assert.NotNull(config.Destination);
		Assert.Equal(@"D:\Backup\Pictures", config.Destination.Path);
		Assert.Equal("preserve-hierarchy", config.Destination.OutputStructure);
		Assert.Equal("", config.Destination.CustomOutputPattern);

		Assert.NotNull(config.Collision);
		Assert.Equal("rename", config.Collision.Strategy);
		Assert.Equal("size-and-modified-time", config.Collision.Comparison);
		Assert.Equal("append-number", config.Collision.RenameStrategy);
		Assert.Equal("", config.Collision.CustomPattern);

		Assert.NotNull(config.Metadata);
		Assert.Equal("ini", config.Metadata.SidecarFormat);
		Assert.Equal("none", config.Metadata.IndexType);
		Assert.Equal(["sha256"], config.Metadata.ComparisonHashAlgorithms);
		Assert.Equal(["sha256"], config.Metadata.VerificationHashAlgorithms);
		Assert.True(config.Metadata.EnableMetadata);
		Assert.Equal("hash", config.Metadata.PostWriteVerification);
		Assert.True(config.Metadata.EnableTimestampCorrection);

		Assert.NotNull(config.Behavior);
		Assert.False(config.Behavior.DryRun);
		Assert.True(config.Behavior.StopOnError);
		Assert.Equal(0, config.Behavior.Delay);
		Assert.Equal("continue", config.Behavior.ResumeBehavior);

		Assert.NotNull(config.Execution);
		Assert.Equal(4, config.Execution.MaxDegreeOfParallelism);
	}

	[Fact]
	public void Load_TomlFile_EmptyExecution_DefaultsToNull()
	{
		string toml = @"
name = ""test""
[source]
path = ""C:\\test""
[destination]
path = ""D:\\test""
";
		string tempFile = Path.GetTempFileName() + ".toml";
		try
		{
			File.WriteAllText(tempFile, toml);
			BackupPlan4Config config = BackupPlan4Loader.Load(new FileInfo(tempFile));

			Assert.NotNull(config.Execution);
			Assert.Null(config.Execution.MaxDegreeOfParallelism);
		}
		finally
		{
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void Load_TomlFile_UnknownValues_UseDefaults()
	{
		string toml = @"
name = ""test""
[source]
path = ""C:\\test""
[destination]
path = ""D:\\test""
[collision]
strategy = ""unknown""
";
		string tempFile = Path.GetTempFileName() + ".toml";
		try
		{
			File.WriteAllText(tempFile, toml);
			BackupPlan4Config config = BackupPlan4Loader.Load(new FileInfo(tempFile));

			Assert.Equal("unknown", config.Collision.Strategy);
		}
		finally
		{
			File.Delete(tempFile);
		}
	}

	// ----------------------------------------------------------------
	// JSON config loading
	// ----------------------------------------------------------------

	[Fact]
	public void Load_JsonFile_ParsesAllSections()
	{
		string json = @"
{
  ""name"": ""Pictures backup"",
  ""source"": {
    ""type"": ""filesystem"",
    ""path"": ""C:\\Users\\John\\Pictures"",
    ""recursive"": true,
    ""includePatterns"": [""*.jpg"", ""*.jpeg"", ""*.png""],
    ""excludePatterns"": [""*.tmp""]
  },
  ""destination"": {
    ""path"": ""D:\\Backup\\Pictures"",
    ""outputStructure"": ""preserve-hierarchy""
  },
  ""collision"": {
    ""strategy"": ""rename"",
    ""comparison"": ""hash"",
    ""renameStrategy"": ""timestamp""
  },
  ""metadata"": {
    ""sidecarFormat"": ""json"",
    ""indexType"": ""none"",
    ""enableMetadata"": true,
    ""postWriteVerification"": ""hash"",
    ""enableTimestampCorrection"": true
  },
  ""behavior"": {
    ""dryRun"": true,
    ""stopOnError"": false,
    ""delay"": 100
  },
  ""execution"": {
    ""maxDegreeOfParallelism"": 2
  }
}
";
		string tempFile = Path.GetTempFileName() + ".json";
		try
		{
			File.WriteAllText(tempFile, json);
			BackupPlan4Config config = BackupPlan4Loader.Load(new FileInfo(tempFile));

			Assert.NotNull(config);
			Assert.Equal("Pictures backup", config.Name);

			Assert.Equal("filesystem", config.Source.Type);
			Assert.Equal(@"C:\Users\John\Pictures", config.Source.Path);
			Assert.True(config.Source.Recursive);

			Assert.Equal("preserve-hierarchy", config.Destination.OutputStructure);

			Assert.Equal("rename", config.Collision.Strategy);
			Assert.Equal("hash", config.Collision.Comparison);
			Assert.Equal("timestamp", config.Collision.RenameStrategy);

			Assert.Equal("json", config.Metadata.SidecarFormat);
			Assert.True(config.Metadata.EnableMetadata);

			Assert.True(config.Behavior.DryRun);
			Assert.False(config.Behavior.StopOnError);
			Assert.Equal(100, config.Behavior.Delay);

			Assert.Equal(2, config.Execution.MaxDegreeOfParallelism);
		}
		finally
		{
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void Load_InvalidExtension_ThrowsNotSupported()
	{
		string tempFile = Path.GetTempFileName() + ".yaml";
		try
		{
			File.WriteAllText(tempFile, "dummy");
			var ex = Assert.Throws<NotSupportedException>(() => BackupPlan4Loader.Load(new FileInfo(tempFile)));
			Assert.Contains(".yaml", ex.Message);
		}
		finally
		{
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void Load_FileNotFound_ThrowsFileNotFoundException()
	{
		var ex = Assert.Throws<FileNotFoundException>(() =>
			BackupPlan4Loader.Load(new FileInfo(@"Z:\nonexistent\file.toml")));
		Assert.Contains("not found", ex.Message);
	}

	[Fact]
	public void Load_NullFile_ThrowsArgumentNullException()
	{
		Assert.Throws<ArgumentNullException>(() => BackupPlan4Loader.Load(null!));
	}
}
