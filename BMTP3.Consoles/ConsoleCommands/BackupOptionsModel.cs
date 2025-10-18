using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Consoles.ConsoleCommands;
public class BackupOptionsModel : BaseOptionsModel
{
	public static Option<int> DelayOption { get; } = new("--delay", "-w", "--wait")
	{
		Description = "Delay between lines, specified as milliseconds per character in a line.", 
		DefaultValueFactory = parseResult => 42,
		Arity = ArgumentArity.ZeroOrOne
	};
	public int Delay { get; set; }

	public static Option<bool> SimulateOption { get; } = new("--dry-run", "-n", "--simulate")
	{
		Description = "Simulates the backup operation without making any changes. Shows what would happen if the command is executed.",
		Arity = ArgumentArity.ZeroOrOne
	};
	public bool Simulate { get; set; }

	public static Option<FileInfo> ConfigOption { get; } = new("--config", "-c")
	{
		Description = "Path to the backup configuration file to use.",
		DefaultValueFactory = parseResult => new FileInfo("default.toml")
	};

	public FileInfo? Config { get; set; }
	public static Option<string> SourceDeviceOption { get; } = new("--source-device", "-d") {
		Description = "Name of the media device to backup from (e.g. 'Apple iPhone').",
		Arity = ArgumentArity.ZeroOrOne
	};
	public string? SourceDevice { get; set; }
	public static Option<string> SourceFolderOption { get; } = new("--source-folder", "-f") {
		Description = "Source folder on the device (e.g. 'Internal Storage/DCIM/100APPLE').",
		Arity = ArgumentArity.ZeroOrOne
	};
	public string? SourceFolder { get; set; }

	public static Option<DirectoryInfo> OutputOption { get; } = new("--output", "-o") {
		Description = "Destination folder for the backup.",
		Arity = ArgumentArity.ZeroOrOne
	};
	public DirectoryInfo? Output { get; set; }
	public static Option<bool> CompareBinaryOption { get; } = new("--compare-binary", "-b") {
		Description = "Compare files by binary content.",
		Arity = ArgumentArity.ZeroOrOne
	};
	public bool CompareBinary { get; set; }

	public static Option<bool> RecursiveOption { get; } = new("--recursive", "-r") {
		Description = "Recursively include subfolders.",
		Arity = ArgumentArity.ZeroOrOne
	};
	public bool Recursive { get; set; }
	public static Option<string> CollisionStrategyOption { get; } = new Option<string>("--collision-strategy", "-c") {
		Description = "Strategy for file name collisions: increment, overwrite, skip, error.",
		Arity = ArgumentArity.ZeroOrOne,
		DefaultValueFactory = argumentResult => "increment"
	}.AcceptOnlyFromAmong("increment", "overwrite", "skip", "error");
	public string? CollisionStrategy { get; set; }
	public static Option<string> FilePatternOption { get; } = new("--file-pattern", "-p") {
		Description = "Pattern for naming backup files.",
		Arity = ArgumentArity.ZeroOrOne
	};
	public string? FilePattern { get; set; }

	public static Option<string> FilePatternIfExistOption { get; } = new("--file-pattern-if-exist", "-e") {
		Description = "Pattern for naming backup files if file already exists.",
		Arity = ArgumentArity.ZeroOrOne
	};
	public string? FilePatternIfExist { get; set; }
}
