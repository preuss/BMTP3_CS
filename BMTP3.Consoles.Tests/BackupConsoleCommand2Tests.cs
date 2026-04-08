using System.CommandLine;
using BMTP3.Consoles.ConsoleCommands;
using BMTP3.Core2.BackupNew.Api.Request.Enums;

namespace BMTP3.Consoles.Tests;

/// <summary>
///     Tests for BackupOptionsModel property assignment and model behaviour.
///     We test the model directly (property get/set) rather than round-tripping through
///     System.CommandLine CLI parsing, because the static Option&lt;T&gt; instances on
///     BackupOptionsModel are shared across all tests and System.CommandLine v2 ties a
///     ParseResult to the exact Command instance that produced it, which makes cross-test
///     isolation impossible without resetting static state.
/// </summary>
public class BackupConsoleCommand2Tests
{
	// ----------------------------------------------------------------
	// Source directory / source device
	// ----------------------------------------------------------------

	[Fact]
	public void SourceDirectory_WhenSet_ReturnsSameValue()
	{
		BackupOptionsModel model = new();
		model.SourceDirectory = @"C:\Users\Alice\Pictures";

		Assert.Equal(@"C:\Users\Alice\Pictures", model.SourceDirectory);
	}

	[Fact]
	public void SourceDevice_WhenSet_ReturnsSameValue()
	{
		BackupOptionsModel model = new();
		model.SourceDevice = "Apple iPhone";

		Assert.Equal("Apple iPhone", model.SourceDevice);
	}

	[Fact]
	public void SourceDirectory_DefaultsToNull()
	{
		BackupOptionsModel model = new();

		Assert.Null(model.SourceDirectory);
	}

	[Fact]
	public void SourceDevice_DefaultsToNull()
	{
		BackupOptionsModel model = new();

		Assert.Null(model.SourceDevice);
	}

	// ----------------------------------------------------------------
	// Include / exclude patterns
	// ----------------------------------------------------------------

	[Fact]
	public void IncludePatterns_WhenPopulated_ContainsAddedPatterns()
	{
		BackupOptionsModel model = new();
		model.IncludePatterns.Add("*.jpg");
		model.IncludePatterns.Add("*.png");

		Assert.Contains("*.jpg", model.IncludePatterns);
		Assert.Contains("*.png", model.IncludePatterns);
		Assert.Equal(2, model.IncludePatterns.Count);
	}

	[Fact]
	public void ExcludePatterns_WhenPopulated_ContainsAddedPatterns()
	{
		BackupOptionsModel model = new();
		model.ExcludePatterns.Add("*.tmp");
		model.ExcludePatterns.Add("Thumbs.db");

		Assert.Contains("*.tmp", model.ExcludePatterns);
		Assert.Contains("Thumbs.db", model.ExcludePatterns);
		Assert.Equal(2, model.ExcludePatterns.Count);
	}

	[Fact]
	public void IncludePatterns_DefaultsToEmptyList()
	{
		BackupOptionsModel model = new();

		Assert.NotNull(model.IncludePatterns);
		Assert.Empty(model.IncludePatterns);
	}

	[Fact]
	public void ExcludePatterns_DefaultsToEmptyList()
	{
		BackupOptionsModel model = new();

		Assert.NotNull(model.ExcludePatterns);
		Assert.Empty(model.ExcludePatterns);
	}

	// ----------------------------------------------------------------
	// Simulate / dry-run flag
	// ----------------------------------------------------------------

	[Fact]
	public void Simulate_WhenSetTrue_ReturnsTrue()
	{
		BackupOptionsModel model = new();
		model.Simulate = true;

		Assert.True(model.Simulate);
	}

	[Fact]
	public void Simulate_DefaultsToFalse()
	{
		BackupOptionsModel model = new();

		Assert.False(model.Simulate);
	}

	// ----------------------------------------------------------------
	// Output directory
	// ----------------------------------------------------------------

	[Fact]
	public void OutputDirectory_WhenSet_ReturnsSameDirectoryInfo()
	{
		BackupOptionsModel model = new();
		DirectoryInfo dir = new(@"D:\Backups\2026");
		model.OutputDirectory = dir;

		Assert.NotNull(model.OutputDirectory);
		Assert.Contains("2026", model.OutputDirectory!.FullName);
	}

	[Fact]
	public void OutputDirectory_DefaultsToNull()
	{
		BackupOptionsModel model = new();

		Assert.Null(model.OutputDirectory);
	}

	// ----------------------------------------------------------------
	// Option metadata (option names / aliases present on static options)
	// ----------------------------------------------------------------

	[Fact]
	public void SimulateOption_HasDryRunAlias()
	{
		// Verify the static option is configured with the expected CLI names
		List<string> aliases = BackupOptionsModel.SimulateOption.Aliases.ToList();
		List<string> allNames = new[] { BackupOptionsModel.SimulateOption.Name }.Concat(aliases).ToList();

		Assert.Contains("--dry-run", allNames);
	}

	[Fact]
	public void SimulateOption_HasSimulateAlias()
	{
		List<string> aliases = BackupOptionsModel.SimulateOption.Aliases.ToList();
		List<string> allNames = new[] { BackupOptionsModel.SimulateOption.Name }.Concat(aliases).ToList();

		Assert.Contains("--simulate", allNames);
	}

	[Fact]
	public void SourceDirectoryOption_HasExpectedName()
	{
		Assert.Equal("--source-directory", BackupOptionsModel.SourceDirectoryOption.Name);
	}

	[Fact]
	public void IncludePatternsOption_HasExpectedName()
	{
		Assert.Equal("--include", BackupOptionsModel.IncludePatternsOption.Name);
	}

	[Fact]
	public void ExcludePatternsOption_HasExpectedName()
	{
		Assert.Equal("--exclude", BackupOptionsModel.ExcludePatternsOption.Name);
	}

	// ----------------------------------------------------------------
	// GetAllOptions returns expected count and non-null entries
	// ----------------------------------------------------------------

	[Fact]
	public void GetAllOptions_ReturnsNonEmptyList()
	{
		BackupOptionsModel model = new();
		List<Option> options = model.GetAllOptions();

		Assert.NotNull(options);
		Assert.NotEmpty(options);
	}

	[Fact]
	public void GetAllOptions_AllEntriesAreNonNull()
	{
		BackupOptionsModel model = new();
		List<Option> options = model.GetAllOptions();

		Assert.All(options, o => Assert.NotNull(o));
	}

	// ----------------------------------------------------------------
	// Collision / rename strategy defaults
	// ----------------------------------------------------------------

	[Fact]
	public void CollisionResolutionType_DefaultsToRename()
	{
		BackupOptionsModel model = new();
		// Default is set via DefaultValueFactory; direct property is value-type so starts at 0
		// We verify the option's DefaultValueFactory returns the expected default
		Assert.Equal(CollisionResolutionType.Rename,
			BackupOptionsModel.CollisionResolutionTypeOption.DefaultValueFactory!(null!));
	}

	[Fact]
	public void RenameStrategy_WhenSetToTimestamp_ReturnsTimestamp()
	{
		BackupOptionsModel model = new();
		model.RenameStrategy = RenameStrategy.Timestamp;

		Assert.Equal(RenameStrategy.Timestamp, model.RenameStrategy);
	}

	// ----------------------------------------------------------------
	// Recursive flag
	// ----------------------------------------------------------------

	[Fact]
	public void Recursive_WhenSetFalse_ReturnsFalse()
	{
		BackupOptionsModel model = new();
		model.Recursive = false;

		Assert.False(model.Recursive);
	}

	// ----------------------------------------------------------------
	// Verification options defaults
	// ----------------------------------------------------------------

	[Fact]
	public void PostWriteVerification_DefaultsToHash()
	{
		BackupOptionsModel model = new();
		Assert.Equal(PostWriteVerificationType.Hash,
			BackupOptionsModel.PostWriteVerificationOption.DefaultValueFactory!(null!));
	}

	[Fact]
	public void VerificationRetryCount_DefaultsTo1()
	{
		BackupOptionsModel model = new();
		Assert.Equal(1,
			BackupOptionsModel.VerificationRetryCountOption.DefaultValueFactory!(null!));
	}

	[Fact]
	public void VerificationRetryDelayMs_DefaultsTo250()
	{
		BackupOptionsModel model = new();
		Assert.Equal(250,
			BackupOptionsModel.VerificationRetryDelayMsOption.DefaultValueFactory!(null!));
	}

	[Fact]
	public void VerificationDeleteOnFailure_DefaultsToFalse()
	{
		BackupOptionsModel model = new();
		Assert.False(model.VerificationDeleteOnFailure);
	}

	[Fact]
	public void VerificationTimeoutMs_DefaultsTo0()
	{
		BackupOptionsModel model = new();
		Assert.Equal(0,
			BackupOptionsModel.VerificationTimeoutMsOption.DefaultValueFactory!(null!));
	}

	[Fact]
	public void VerificationRetryCountOption_HasExpectedName()
	{
		Assert.Equal("--verify-retry-count", BackupOptionsModel.VerificationRetryCountOption.Name);
	}

	[Fact]
	public void VerificationRetryDelayMsOption_HasExpectedName()
	{
		Assert.Equal("--verify-retry-delay", BackupOptionsModel.VerificationRetryDelayMsOption.Name);
	}

	[Fact]
	public void VerificationDeleteOnFailureOption_HasExpectedName()
	{
		Assert.Equal("--verify-delete-on-failure", BackupOptionsModel.VerificationDeleteOnFailureOption.Name);
	}

	[Fact]
	public void VerificationTimeoutMsOption_HasExpectedName()
	{
		Assert.Equal("--verify-timeout", BackupOptionsModel.VerificationTimeoutMsOption.Name);
	}

	[Fact]
	public void PostWriteVerificationOption_HasExpectedName()
	{
		Assert.Equal("--verify", BackupOptionsModel.PostWriteVerificationOption.Name);
	}
}