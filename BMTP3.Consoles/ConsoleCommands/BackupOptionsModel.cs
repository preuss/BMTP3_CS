using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BMTP3.Consoles.ParserElements;

namespace BMTP3.Consoles.ConsoleCommands;
public class BackupOptionsModel : BaseOptionsModel {
	// --------------------------------------------------
	// BASIC
	// --------------------------------------------------

	public static Option<FileInfo> ConfigOption { get; } = new("--config", "-c") {
		Description = "Path to the backup configuration file (TOML or JSON).",
		DefaultValueFactory = parseResult => new FileInfo("default.toml"),
	};
	public FileInfo? Config { get; set; }

	public static Option<string> SourceDeviceOption { get; } = new("--source-device", "-d") {
		Description = "Name of the source device to backup from (e.g. 'Apple iPhone').",
		Arity = ArgumentArity.ZeroOrOne
	};
	public string? SourceDevice { get; set; }
	public static Option<string> SourceDirectoryOption { get; } = new("--source-directory", "-s") {
		Description = "Source folder on the device (e.g. 'Internal Storage/DCIM/100APPLE' or 'C:\\Users\\Bob\\Pictures').",
		Arity = ArgumentArity.ZeroOrOne
	};
	public string? SourceDirectory { get; set; }

	public static Option<DirectoryInfo> OutputDirectoryOption { get; } = new("--output", "-o") {
		Description = "Destination folder for the backup.",
		Arity = ArgumentArity.ZeroOrOne
	};
	public DirectoryInfo? OutputDirectory { get; set; }

	public static Option<bool> RecursiveOption { get; } = new("--recursive", "-r") {
		Description = "Include subfolders recursively.",
		DefaultValueFactory = parseResult => true,
		Arity = ArgumentArity.ZeroOrOne
	};
	public bool Recursive { get; set; }


	// --------------------------------------------------
	// OUTPUT STRUCTURE
	// --------------------------------------------------
	public static Option<OutputStructureStrategies> OutputStrategyOption { get; } = new("--output-structure") {
		Description = "Defines how destination folders are structured: PreserveSourceTree, Flat, or CustomPathPattern.",
		Arity = ArgumentArity.ZeroOrOne,
		DefaultValueFactory = argumentResult => OutputStructureStrategies.PreserveSourceTree
	};
	public OutputStructureStrategies OutputStrategy { get; set; }

	public static Option<string> CustomOutputFilePathOption { get; } = new("--path-pattern") {
		Description = "Custom path or filename pattern (used if OutputStrategy = CustomPathPattern).",
		Arity = ArgumentArity.ZeroOrOne
	};
	public string? CustomOutputFilePath { get; set; }


	// --------------------------------------------------
	// COLLISION HANDLING
	// --------------------------------------------------
	public static Option<CollisionResolutionTypes> CollisionResolutionTypeOption { get; } = new("--collision-resolution") {
			Description = "Defines how to handle existing files: Overwrite, Skip, Error, or Rename.",
			Arity = ArgumentArity.ZeroOrOne,
			DefaultValueFactory = argumentResult => CollisionResolutionTypes.Rename
		};
	public CollisionResolutionTypes CollisionResolutionType { get; set; }

	public static Option<CollisionComparisonTypes> CollisionComparisonOption { get; } = new("--collision-compare") {
		Description = "Defines how to compare existing files before applying resolution: None, Hash, or Binary.",
		Arity = ArgumentArity.ZeroOrOne,
		DefaultValueFactory = argumentResult => CollisionComparisonTypes.Binary
	};
	public CollisionComparisonTypes CollisionComparison { get; set; }

	public static Option<RenameStrategies> RenameStrategyOption { get; } = new("--rename-strategy") {
		Description = "Defines rename behavior when collision resolution is 'Rename': Increment, Timestamp, Hash, or CustomCollisionPathPattern.",
		Arity = ArgumentArity.ZeroOrOne,
		DefaultValueFactory = argumentResult => RenameStrategies.Increment
	};
	public RenameStrategies RenameStrategy { get; set; }

	public static Option<string> CustomCollisionOutputFilePathOption { get; } = new("--collision-pattern") {
		Description = "Custom pattern used when RenameStrategy = CustomCollisionPathPattern.",
		Arity = ArgumentArity.ZeroOrOne
	};
	public string? CustomCollisionOutputFilePath { get; set; }

	// --------------------------------------------------
	// FILTERING (Include/Exclude)
	// --------------------------------------------------

	public static Option<List<string>> IncludePatternsOption { get; } = new("--include") {
		Description = "Glob patterns of files or folders to explicitly include (comma-separated).",
		Arity = ArgumentArity.ZeroOrMore
	};
	public List<string> IncludePatterns { get; set; } = new();


	public static Option<List<string>> ExcludePatternsOption { get; } = new("--exclude") {
		Description = "Glob patterns of files or folders to exclude (comma-separated).",
		Arity = ArgumentArity.ZeroOrMore
	};
	public List<string> ExcludePatterns { get; set; } = new();

	// --------------------------------------------------
	// METADATA
	// --------------------------------------------------

	public static Option<SidecarFormats> SidecarFormatOption { get; } = new("--sidecar-format") {
		Description = "Format of per-file sidecar metadata: None, Ini, or Json.",
		Arity = ArgumentArity.ZeroOrOne,
		DefaultValueFactory = argumentResult => SidecarFormats.Ini
	};

	public SidecarFormats SidecarFormat { get; set; }

	public static Option<BackupIndexTypes> BackupIndexTypeOption { get; } = new("--backup-index") {
		Description = "Type of centralized backup index: None, Json, or Database.",
		Arity = ArgumentArity.ZeroOrOne,
		DefaultValueFactory = argumentResult => BackupIndexTypes.Json
	};
	public BackupIndexTypes BackupIndexType { get; set; }

	// --------------------------------------------------
	// EXECUTION
	// --------------------------------------------------
    public static Option<bool> SimulateOption { get; } = new("--dry-run", "-n", "--simulate") {
		Description = "Simulates the backup without performing any write operations."
	};
	public bool Simulate { get; set; }

	public static Option<int> DelayOption { get; } = new("--delay", "-w", "--wait") {
		Description = "Delay between file operations, in milliseconds (useful for throttling or testing).",
		DefaultValueFactory = parseResult => 42,
	};
	public int Delay { get; set; }

	protected override void DoAddValidators()
	{
		
	}
}