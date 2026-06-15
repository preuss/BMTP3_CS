using System.CommandLine;
using BMTP3.Core4.Api;
using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Api.Models.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace BMTP3.Consoles.ConsoleCommands;

public class BackupConsoleCommand4ListDrives : BaseConsoleCommand
{
	public BackupConsoleCommand4ListDrives() : this(
		"list-sources",
		"List available backup sources (drives and MTP devices)",
		new GlobalOptionsModel()
	)
	{ }

	public BackupConsoleCommand4ListDrives(string name, string description, GlobalOptionsModel globalOptions) : base(name, description, globalOptions)
	{ }

	public required IServiceProvider ServiceProvider { get; init; }

	protected override Task<int> DoExecuteAsync(
		ParseResult parseResult,
		CancellationToken cancellationToken
	)
	{
		ILogger<BackupConsoleCommand4ListDrives> logger = ServiceProvider.GetRequiredService<ILogger<BackupConsoleCommand4ListDrives>>();

		IAnsiConsole console = ServiceProvider.GetService<IAnsiConsole>() ?? AnsiConsole.Console;

		IDriveCatalogService? catalogService = ServiceProvider.GetService<IDriveCatalogService>();

		if (catalogService == null)
		{
			console.WriteLine("Drive catalog service is not available.");
			logger.LogError("Drive catalog service is not available.");
			return Task.FromResult(1);
		}

		IReadOnlyList<DriveCatalogEntry> drives = catalogService.ListDrives();

		if (drives.Count == 0)
		{
			console.WriteLine("No backup sources found.");
			logger.LogInformation("No backup sources found.");
			return Task.FromResult(0);
		}

		List<DriveCatalogEntry> fileSystemDrives = drives.Where(d => d.SourceType == BackupSourceType.FileSystem).ToList();
		List<DriveCatalogEntry> mediaDrives = drives.Where(d => d.SourceType == BackupSourceType.MediaDevice).ToList();

		bool hasPrintedTable = false;

		if (fileSystemDrives.Count > 0)
		{
			PrintDriveTable(console, fileSystemDrives, "File system drives", "Local and mounted file system sources.");

			hasPrintedTable = true;
		}

		if (mediaDrives.Count > 0)
		{
			if(hasPrintedTable) console.WriteLine();

			PrintDriveTable(console, mediaDrives, "Media devices", "Connected MTP/media device sources.");
		}

		return Task.FromResult(0);
	}

	private static void PrintDriveTable(
		IAnsiConsole console,
		IReadOnlyList<DriveCatalogEntry> drives,
		string title = "",
		string caption = ""
	)
	{
		Table table = new Table();

		if(!string.IsNullOrWhiteSpace(title)) table.Title(title);

		if(!string.IsNullOrWhiteSpace(caption)) table.Caption(caption);

		table
			.Border(TableBorder.Minimal)
			.AddColumn("Label (Id)")
			.AddColumn(new TableColumn("Type"))
			.AddColumn("Root Path")
			.AddColumn(new TableColumn("Total Size").RightAligned())
			.AddColumn(new TableColumn("Free Space").RightAligned());

		foreach (DriveCatalogEntry d in drives)
		{
			table.AddRow(
				new Text(d.Name),
				new Text(FormatSourceType(d.SourceType)),
				new Text(d.RootPath),
				new Text(FormatSize(d.TotalSize)),
				new Text(FormatSize(d.AvailableFreeSpace))
			);
		}

		console.Write(table);
	}

	private static string FormatSourceType(BackupSourceType sourceType)
	{
		return sourceType switch
		{
			BackupSourceType.FileSystem => "File system",
			BackupSourceType.MediaDevice => "Media device",
			_ => sourceType.ToString()
		};
	}

	private static string FormatSize(long bytes, bool showTerabyte = false)
	{
		string retVal = "Unknown";

		bool useTerabyte = showTerabyte && bytes >= 1024L * 1024 * 1024 * 1024;

		if (useTerabyte)
			retVal = $"{bytes / (1024.0 * 1024 * 1024 * 1024):F1} TB";
		else
			retVal = $"{bytes / (1024.0 * 1024 * 1024):F1} GB";

		if (bytes < 1024L * 1024 * 1024)
			retVal = $"{bytes / (1024.0 * 1024):F1} MB";

		if (bytes < 1024L * 1024)
			retVal = $"{bytes / 1024.0:F1} KB";

		if (bytes < 1024)
			retVal = $"{bytes} B";

		if (bytes < 0)
			retVal = "Unknown";

		return retVal;
	}
}