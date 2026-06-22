using BMTP3.Core4.Api.Models.Enums;
using System.CommandLine;
using System.CommandLine.Parsing;

namespace BMTP3.Consoles.ConsoleCommands.Core4;

public class BackupOptionsModel4 : BaseOptionsModel
{
	// --------------------------------------------------
	// BASIC
	// --------------------------------------------------

	public static Option<FileInfo> ConfigOption { get; } = new("--config", "-c")
	{
		Description = "Path to the backup configuration file (TOML, JSON or JSON5).",
		Arity = ArgumentArity.ZeroOrOne
	};

	public FileInfo? Config { get; set; }

	// Reserved for future option result value validation (e.g., verifying that --config resolves before other options)
	public OptionResult? ConfigOptionResult { get; set; }

	public static Option<string> NameOption { get; } = new("--name")
	{
		Description = "Friendly name for this backup job (e.g. 'iPhone Photos'). Used in logs and reports.",
		Validators = { result =>
			{
				if(result.Tokens.Count > 0 && string.IsNullOrWhiteSpace(result.Tokens[0].Value))
					result.AddError("--name cannot be empty or whitespace.");
			}
		}
	};

	public string? Name { get; set; }

	public static Option<string> SourcePathOption { get; } = new("--source-path", "-s")
	{
		Description =
			"Absolute source path. For MTP, format: mtp://{DeviceName}/{DriveName}/{DirectoryPath} (e.g., 'mtp://Apple iPad/Internal Storage/DCIM/100APPLE'). For filesystem, use a local path (e.g., 'C:\\Users\\Bob\\Pictures')."
	};

	public string? SourcePath { get; set; }

	public static Option<DirectoryInfo> OutputDirectoryOption { get; } = new("--output", "-o")
	{
		Description = "Destination folder for the backup."
	};

	public DirectoryInfo? OutputDirectory { get; set; }

	public static Option<bool> RecursiveOption { get; } = new("--recursive", "-r")
	{
		Description = "Include subfolders recursively.",
		DefaultValueFactory = parseResult => true
	};

	public bool Recursive { get; set; } = true;


	// --------------------------------------------------
	// OUTPUT STRUCTURE
	// --------------------------------------------------
	public static Option<OutputStructureStrategy> OutputStrategyOption { get; } = new("--output-structure")
	{
		Description = "Defines how destination folders are structured: PreserveHierarchy, Flat, or CustomPathPattern.",
		DefaultValueFactory = argumentResult => OutputStructureStrategy.PreserveHierarchy
	};

	public OutputStructureStrategy OutputStrategy { get; set; } = OutputStructureStrategy.PreserveHierarchy;

	public static Option<string> CustomOutputFilePathOption { get; } = new("--path-pattern")
	{
		Description = "Custom path or filename pattern (used if OutputStrategy = CustomPathPattern)."
	};

	public string? CustomOutputFilePath { get; set; }


	// --------------------------------------------------
	// COLLISION HANDLING
	// --------------------------------------------------
	public static Option<CollisionStrategy> CollisionStrategyOption { get; } = new("--collision-strategy")
	{
		Description = "Defines how to handle existing files: Overwrite, Skip, Error, or Rename.",
		DefaultValueFactory = argumentResult => CollisionStrategy.Rename
	};

	public CollisionStrategy CollisionStrategy { get; set; } = CollisionStrategy.Rename;

	public static Option<CollisionComparisonType> CollisionComparisonOption { get; } = new("--collision-compare")
	{
		Description = "Defines how to compare existing files before applying resolution: None, Hash, Binary, or SizeAndModifiedTime.",
		Arity = ArgumentArity.ZeroOrOne,
		DefaultValueFactory = argumentResult => CollisionComparisonType.Binary
	};

	public CollisionComparisonType CollisionComparison { get; set; } = CollisionComparisonType.Binary;

	public static Option<RenameStrategy> RenameStrategyOption { get; } = new("--rename-strategy")
	{
		Description =
			"Defines rename behavior when collision resolution is 'Rename': Increment, Timestamp, Hash, or CustomPattern.",
		Arity = ArgumentArity.ZeroOrOne,
		DefaultValueFactory = argumentResult => RenameStrategy.Increment
	};

	public RenameStrategy RenameStrategy { get; set; } = RenameStrategy.Increment;

	public static Option<string> CustomCollisionOutputFilePathOption { get; } = new("--collision-pattern")
	{
		Description = "Custom pattern used when RenameStrategy = CustomPattern."
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

	public SidecarFormat SidecarFormat { get; set; } = SidecarFormat.Ini;

	public static Option<BackupIndexType> BackupIndexTypeOption { get; } = new("--backup-index")
	{
		Description = "Type of centralized backup index: None, Json, or Database.",
		Arity = ArgumentArity.ZeroOrOne,
		DefaultValueFactory = argumentResult => BackupIndexType.None
	};

	public BackupIndexType BackupIndexType { get; set; } = BackupIndexType.None;

	// --------------------------------------------------
	// EXECUTION
	// --------------------------------------------------
	public static Option<int> DelayOption { get; } = new("--delay", "-w", "--wait")
	{
		Description = "Delay in milliseconds between each file. Use to slow down the backup for visual progress. -1 disables, 0 is fastest, >0 is N ms.",
		DefaultValueFactory = _ => 0,
		Arity = ArgumentArity.ZeroOrOne
	};

	public int Delay { get; set; } = 0;

	public static Option<bool> SimulateOption { get; } = new("--dry-run", "-n", "--simulate")
	{
		Description = "Simulates the backup without performing any write operations."
	};

	public bool Simulate { get; set; }

	public static Option<bool> StopOnErrorOption { get; } = new("--stop-on-error")
	{
		Description = "Stop the backup immediately on first error (fail-fast). Set to false to continue on errors.",
		DefaultValueFactory = _ => true
	};

	public bool StopOnError { get; set; } = true;

	public static Option<SessionResumeStrategy> ResumeBehaviorOption { get; } = new("--resume-behavior")
	{
		Description = "How to handle a session mismatch (file list changed since last run): Abort (default), Restart (delete old session and start over), or Continue (reconcile and resume anyway).",
		Arity = ArgumentArity.ZeroOrOne,
		DefaultValueFactory = _ => SessionResumeStrategy.Abort
	};

	public SessionResumeStrategy ResumeBehavior { get; set; } = SessionResumeStrategy.Abort;

	public static Option<ItemIdScope> ItemIdScopeOption { get; } = new("--item-id-scope")
	{
		Description = "How files are identified during backup: " +
			"Session (ObjectId, unique within Connect() only), " +
			"Connection (PUID, stable across Connect/Disconnect — default), " +
			"or Persistent (generated from file metadata, independent of WPD).",
		Arity = ArgumentArity.ZeroOrOne,
		DefaultValueFactory = _ => ItemIdScope.Connection,
	};

	public ItemIdScope ItemIdScope { get; set; } = ItemIdScope.Connection;

	// --------------------------------------------------
	// TIMESTAMP
	// --------------------------------------------------

	public static Option<bool> EnableTimestampCorrectionOption { get; } = new("--enable-timestamp-correction")
	{
		Description = "Enable correction of file timestamps from metadata (EXIF, XMP, etc.). Set to false to use filesystem timestamps only.",
		DefaultValueFactory = _ => true
	};

	public bool EnableTimestampCorrection { get; set; } = true;

	// --------------------------------------------------
	// VERIFICATION
	// --------------------------------------------------

	public static Option<PostWriteVerificationType> PostWriteVerificationOption { get; } = new("--verify")
	{
		Description = "Post-write verification method: None, or Hash.",
		Arity = ArgumentArity.ZeroOrOne,
		DefaultValueFactory = argumentResult => PostWriteVerificationType.None
	};

	public PostWriteVerificationType PostWriteVerification { get; set; } = PostWriteVerificationType.None;

	// --------------------------------------------------
	// HASH ALGORITHMS
	// --------------------------------------------------

	public static Option<List<string>> ComparisonHashOption { get; } = new("--comparison-hash")
	{
		Description = "Hash algorithms for collision comparison (e.g. '--comparison-hash sha256 --comparison-hash blake3-256'). Defaults to all supported algorithms.",
		Arity = ArgumentArity.OneOrMore
	};

	public List<string> ComparisonHash { get; set; } = new();

	public static Option<List<string>> VerificationHashOption { get; } = new("--verification-hash")
	{
		Description = "Hash algorithms for post-write verification (e.g. '--verification-hash sha512'). Defaults to all supported algorithms.",
		Arity = ArgumentArity.OneOrMore
	};

	public List<string> VerificationHash { get; set; } = new();

	// Placeholder for future cross-option validators (e.g., --path-pattern requires --output-structure CustomPathPattern)
	protected override void DoAddValidators()
	{
	}
}
