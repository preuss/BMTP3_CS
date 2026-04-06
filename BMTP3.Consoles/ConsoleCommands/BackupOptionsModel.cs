using BMTP3.Core2.BackupNew.Api.Request.Enums;
using System.CommandLine;
using System.CommandLine.Parsing;

namespace BMTP3.Consoles.ConsoleCommands;
public class BackupOptionsModel : BaseOptionsModel
{
	// --------------------------------------------------
	// BASIC
	// --------------------------------------------------

	public static Option<FileInfo> ConfigOption { get; } = new("--config", "-c")
	{
		Description = "Path to the backup configuration file (TOML or JSON).",
		//DefaultValueFactory = parseResult => new FileInfo("default.toml"),
		CustomParser = result => result.Tokens.Count == 0
			? new FileInfo("default.toml")
			: new FileInfo(result.Tokens[0].Value),
		Arity = ArgumentArity.ZeroOrOne
	};
	public FileInfo? Config { get; set; }
	public OptionResult? ConfigOptionResult { get; set; }

	public static Option<string> NameOption { get; } = new("--name")
	{
		Description = "Friendly name for this backup job (e.g. 'iPhone Photos'). Used in logs and reports.",
	};
	public string? Name { get; set; }

	public static Option<string> SourceDeviceOption { get; } = new("--source-device", "-d")
	{
		Description = "Name of the source device to backup from (e.g. 'Apple iPhone').",
	};
	public string? SourceDevice { get; set; }
	public static Option<string> SourceDirectoryOption { get; } = new("--source-directory", "-s")
	{
		Description = "Source folder on the device (e.g., 'Internal Storage/DCIM/100APPLE' or 'C:\\Users\\Bob\\Pictures').",
	};
	public string? SourceDirectory { get; set; }

	public static Option<DirectoryInfo> OutputDirectoryOption { get; } = new("--output", "-o")
	{
		Description = "Destination folder for the backup.",
	};
	public DirectoryInfo? OutputDirectory { get; set; }

	public static Option<bool> RecursiveOption { get; } = new("--recursive", "-r")
	{
		Description = "Include subfolders recursively.",
		DefaultValueFactory = parseResult => true,
	};
	public bool Recursive { get; set; }


	// --------------------------------------------------
	// OUTPUT STRUCTURE
	// --------------------------------------------------
	public static Option<OutputStructureStrategy> OutputStrategyOption { get; } = new("--output-structure")
	{
		Description = "Defines how destination folders are structured: PreserveSourceTree, Flat, or CustomPathPattern.",
		DefaultValueFactory = argumentResult => OutputStructureStrategy.PreserveSourceTree
	};
	public OutputStructureStrategy OutputStrategy { get; set; }

	public static Option<string> CustomOutputFilePathOption { get; } = new("--path-pattern")
	{
		Description = "Custom path or filename pattern (used if OutputStrategy = CustomPathPattern).",
	};
	public string? CustomOutputFilePath { get; set; }


	// --------------------------------------------------
	// COLLISION HANDLING
	// --------------------------------------------------
	public static Option<CollisionResolutionType> CollisionResolutionTypeOption { get; } = new("--collision-resolution")
	{
		Description = "Defines how to handle existing files: Overwrite, Skip, Error, or Rename.",
		DefaultValueFactory = argumentResult => CollisionResolutionType.Rename
	};
	public CollisionResolutionType CollisionResolutionType { get; set; }

	public static Option<CollisionComparisonType> CollisionComparisonOption { get; } = new("--collision-compare")
	{
		Description = "Defines how to compare existing files before applying resolution: None, Hash, or Binary.",
		Arity = ArgumentArity.ZeroOrOne,
		DefaultValueFactory = argumentResult => CollisionComparisonType.Binary
	};
	public CollisionComparisonType CollisionComparison { get; set; }

	public static Option<RenameStrategy> RenameStrategyOption { get; } = new("--rename-strategy")
	{
		Description = "Defines rename behavior when collision resolution is 'Rename': Increment, Timestamp, Hash, or CustomCollisionPathPattern.",
		Arity = ArgumentArity.ZeroOrOne,
		DefaultValueFactory = argumentResult => RenameStrategy.Increment
	};
	public RenameStrategy RenameStrategy { get; set; }

	public static Option<string> CustomCollisionOutputFilePathOption { get; } = new("--collision-pattern")
	{
		Description = "Custom pattern used when RenameStrategy = CustomCollisionPathPattern.",
	};
	public string? CustomCollisionOutputFilePath { get; set; }

	// --------------------------------------------------
	// FILTERING (Include/Exclude)
	// --------------------------------------------------

	public static Option<List<string>> IncludePatternsOption { get; } = new("--include")
	{
		Description = "Glob patterns of files or folders to explicitly include (comma-separated).",
		Arity = ArgumentArity.ZeroOrMore
	};
	public List<string> IncludePatterns { get; set; } = new();


	public static Option<List<string>> ExcludePatternsOption { get; } = new("--exclude")
	{
		Description = "Glob patterns of files or folders to exclude (comma-separated).",
		Arity = ArgumentArity.ZeroOrMore
	};
	public List<string> ExcludePatterns { get; set; } = new();

	// --------------------------------------------------
	// METADATA
	// --------------------------------------------------

	public static Option<SidecarFormat> SidecarFormatOption { get; } = new("--sidecar-format")
	{
		Description = "Format of per-file sidecar metadata: None, Ini, or Json.",
		Arity = ArgumentArity.ZeroOrOne,
		DefaultValueFactory = argumentResult => SidecarFormat.Ini
	};

	public SidecarFormat SidecarFormat { get; set; }

	public static Option<BackupIndexType> BackupIndexTypeOption { get; } = new("--backup-index")
	{
		Description = "Type of centralized backup index: None, Json, or Database.",
		Arity = ArgumentArity.ZeroOrOne,
		DefaultValueFactory = argumentResult => BackupIndexType.Json
	};
	public BackupIndexType BackupIndexType { get; set; }

	// --------------------------------------------------
	// EXECUTION
	// --------------------------------------------------
	public static Option<bool> SimulateOption { get; } = new("--dry-run", "-n", "--simulate")
	{
		Description = "Simulates the backup without performing any write operations."
	};
	public bool Simulate { get; set; }

	public static Option<int> DelayOption { get; } = new("--delay", "-w", "--wait")
	{
		Description = "Delay between file operations, in milliseconds (useful for throttling or testing).",
		DefaultValueFactory = parseResult => 42,
	};
	public int Delay { get; set; }

	// --------------------------------------------------
	// VERIFICATION
	// --------------------------------------------------

	public static Option<PostWriteVerificationType> PostWriteVerificationOption { get; } = new("--verify")
	{
		Description = "Post-write verification method: None, Hash, or Binary.",
		Arity = ArgumentArity.ZeroOrOne,
		DefaultValueFactory = argumentResult => PostWriteVerificationType.Hash
	};
	public PostWriteVerificationType PostWriteVerification { get; set; }

	public static Option<int> VerificationRetryCountOption { get; } = new("--verify-retry-count")
	{
		Description = "Number of verification retry attempts before giving up.",
		DefaultValueFactory = parseResult => 1,
	};
	public int VerificationRetryCount { get; set; }

	public static Option<int> VerificationRetryDelayMsOption { get; } = new("--verify-retry-delay")
	{
		Description = "Delay in milliseconds between verification retry attempts.",
		DefaultValueFactory = parseResult => 250,
	};
	public int VerificationRetryDelayMs { get; set; }

	public static Option<bool> VerificationDeleteOnFailureOption { get; } = new("--verify-delete-on-failure")
	{
		Description = "Delete destination file if verification ultimately fails.",
	};
	public bool VerificationDeleteOnFailure { get; set; }

	public static Option<int> VerificationTimeoutMsOption { get; } = new("--verify-timeout")
	{
		Description = "Timeout in milliseconds for per-operation verification. 0 = no timeout.",
		DefaultValueFactory = parseResult => 0,
	};
	public int VerificationTimeoutMs { get; set; }

	protected override void DoAddValidators()
	{

	}
}