using BMTP3.Consoles.ConsoleCommands;
using BMTP3.Consoles.ParserElements;

namespace BMTP3.Consoles.Tests
{
    /// <summary>
    /// Tests for BackupOptionsModel property assignment and model behaviour.
    /// We test the model directly (property get/set) rather than round-tripping through
    /// System.CommandLine CLI parsing, because the static Option&lt;T&gt; instances on
    /// BackupOptionsModel are shared across all tests and System.CommandLine v2 ties a
    /// ParseResult to the exact Command instance that produced it, which makes cross-test
    /// isolation impossible without resetting static state.
    /// </summary>
    public class BackupConsoleCommand2Tests
    {
        // ----------------------------------------------------------------
        // Source directory / source device
        // ----------------------------------------------------------------

        [Fact]
        public void SourceDirectory_WhenSet_ReturnsSameValue()
        {
            var model = new BackupOptionsModel();
            model.SourceDirectory = @"C:\Users\Alice\Pictures";

            Assert.Equal(@"C:\Users\Alice\Pictures", model.SourceDirectory);
        }

        [Fact]
        public void SourceDevice_WhenSet_ReturnsSameValue()
        {
            var model = new BackupOptionsModel();
            model.SourceDevice = "Apple iPhone";

            Assert.Equal("Apple iPhone", model.SourceDevice);
        }

        [Fact]
        public void SourceDirectory_DefaultsToNull()
        {
            var model = new BackupOptionsModel();

            Assert.Null(model.SourceDirectory);
        }

        [Fact]
        public void SourceDevice_DefaultsToNull()
        {
            var model = new BackupOptionsModel();

            Assert.Null(model.SourceDevice);
        }

        // ----------------------------------------------------------------
        // Include / exclude patterns
        // ----------------------------------------------------------------

        [Fact]
        public void IncludePatterns_WhenPopulated_ContainsAddedPatterns()
        {
            var model = new BackupOptionsModel();
            model.IncludePatterns.Add("*.jpg");
            model.IncludePatterns.Add("*.png");

            Assert.Contains("*.jpg", model.IncludePatterns);
            Assert.Contains("*.png", model.IncludePatterns);
            Assert.Equal(2, model.IncludePatterns.Count);
        }

        [Fact]
        public void ExcludePatterns_WhenPopulated_ContainsAddedPatterns()
        {
            var model = new BackupOptionsModel();
            model.ExcludePatterns.Add("*.tmp");
            model.ExcludePatterns.Add("Thumbs.db");

            Assert.Contains("*.tmp", model.ExcludePatterns);
            Assert.Contains("Thumbs.db", model.ExcludePatterns);
            Assert.Equal(2, model.ExcludePatterns.Count);
        }

        [Fact]
        public void IncludePatterns_DefaultsToEmptyList()
        {
            var model = new BackupOptionsModel();

            Assert.NotNull(model.IncludePatterns);
            Assert.Empty(model.IncludePatterns);
        }

        [Fact]
        public void ExcludePatterns_DefaultsToEmptyList()
        {
            var model = new BackupOptionsModel();

            Assert.NotNull(model.ExcludePatterns);
            Assert.Empty(model.ExcludePatterns);
        }

        // ----------------------------------------------------------------
        // Simulate / dry-run flag
        // ----------------------------------------------------------------

        [Fact]
        public void Simulate_WhenSetTrue_ReturnsTrue()
        {
            var model = new BackupOptionsModel();
            model.Simulate = true;

            Assert.True(model.Simulate);
        }

        [Fact]
        public void Simulate_DefaultsToFalse()
        {
            var model = new BackupOptionsModel();

            Assert.False(model.Simulate);
        }

        // ----------------------------------------------------------------
        // Output directory
        // ----------------------------------------------------------------

        [Fact]
        public void OutputDirectory_WhenSet_ReturnsSameDirectoryInfo()
        {
            var model = new BackupOptionsModel();
            var dir = new DirectoryInfo(@"D:\Backups\2026");
            model.OutputDirectory = dir;

            Assert.NotNull(model.OutputDirectory);
            Assert.Contains("2026", model.OutputDirectory!.FullName);
        }

        [Fact]
        public void OutputDirectory_DefaultsToNull()
        {
            var model = new BackupOptionsModel();

            Assert.Null(model.OutputDirectory);
        }

        // ----------------------------------------------------------------
        // Option metadata (option names / aliases present on static options)
        // ----------------------------------------------------------------

        [Fact]
        public void SimulateOption_HasDryRunAlias()
        {
            // Verify the static option is configured with the expected CLI names
            var aliases = BackupOptionsModel.SimulateOption.Aliases.ToList();
            var allNames = new[] { BackupOptionsModel.SimulateOption.Name }.Concat(aliases).ToList();

            Assert.Contains("--dry-run", allNames);
        }

        [Fact]
        public void SimulateOption_HasSimulateAlias()
        {
            var aliases = BackupOptionsModel.SimulateOption.Aliases.ToList();
            var allNames = new[] { BackupOptionsModel.SimulateOption.Name }.Concat(aliases).ToList();

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
            var model = new BackupOptionsModel();
            var options = model.GetAllOptions();

            Assert.NotNull(options);
            Assert.NotEmpty(options);
        }

        [Fact]
        public void GetAllOptions_AllEntriesAreNonNull()
        {
            var model = new BackupOptionsModel();
            var options = model.GetAllOptions();

            Assert.All(options, o => Assert.NotNull(o));
        }

        // ----------------------------------------------------------------
        // Collision / rename strategy defaults
        // ----------------------------------------------------------------

        [Fact]
        public void CollisionResolutionType_DefaultsToRename()
        {
            var model = new BackupOptionsModel();
            // Default is set via DefaultValueFactory; direct property is value-type so starts at 0
            // We verify the option's DefaultValueFactory returns the expected default
            Assert.Equal(CollisionResolutionTypes.Rename,
                BackupOptionsModel.CollisionResolutionTypeOption.DefaultValueFactory!(null!));
        }

        [Fact]
        public void RenameStrategy_WhenSetToTimestamp_ReturnsTimestamp()
        {
            var model = new BackupOptionsModel();
            model.RenameStrategy = RenameStrategies.Timestamp;

            Assert.Equal(RenameStrategies.Timestamp, model.RenameStrategy);
        }

        // ----------------------------------------------------------------
        // Recursive flag
        // ----------------------------------------------------------------

        [Fact]
        public void Recursive_WhenSetFalse_ReturnsFalse()
        {
            var model = new BackupOptionsModel();
            model.Recursive = false;

            Assert.False(model.Recursive);
        }
    }
}
