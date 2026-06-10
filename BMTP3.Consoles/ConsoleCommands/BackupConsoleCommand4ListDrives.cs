using BMTP3.Core4.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Spectre.Console;
using System.CommandLine;

namespace BMTP3.Consoles.ConsoleCommands;

public class BackupConsoleCommand4ListDrives : BaseConsoleCommand
{
	public BackupConsoleCommand4ListDrives() : base(
		"list-sources",
		"List available backup sources (drives and MTP devices)",
		new GlobalOptionsModel()
	)
	{ }

	public IServiceProvider? ServiceProvider { get; init; }

	protected override async Task<int> DoExecuteAsync(
		ParseResult parseResult,
		CancellationToken cancellationToken
	)
	{
		ILogger<BackupConsoleCommand4ListDrives> logger = ServiceProvider.GetService<ILogger<BackupConsoleCommand4ListDrives>>()
														?? ServiceProvider.GetService<ILoggerFactory>()
														?.CreateLogger<BackupConsoleCommand4ListDrives>()
														?? NullLogger<BackupConsoleCommand4ListDrives>.Instance;

		IAnsiConsole console = ServiceProvider.GetService<IAnsiConsole>() ?? AnsiConsole.Console;

		IDriveCatalogService? catalogService = ServiceProvider.GetService<IDriveCatalogService>();

		if (catalogService == null)
		{
			logger.LogError("Drive catalog service is not available.");
			return 1;
		}

		IReadOnlyList<BMTP3.Core4.Api.Models.DriveCatalogEntry> drives = catalogService.ListDrives();

		if (drives.Count == 0)
		{
			logger.LogInformation("No backup sources found.");
			return 0;
		}

		List<BMTP3.Core4.Api.Models.DriveCatalogEntry> fileSystemDrives = drives.Where(d => d.SourceType == BMTP3.Core4.Api.Models.Enums.BackupSourceType.FileSystem).ToList();
		List<BMTP3.Core4.Api.Models.DriveCatalogEntry> mediaDrives = drives.Where(d => d.SourceType == BMTP3.Core4.Api.Models.Enums.BackupSourceType.MediaDevice).ToList();

		if (fileSystemDrives.Count > 0)
		{
			PrintDriveTable(console, fileSystemDrives, includeIdColumn: false);
		}

		if (mediaDrives.Count > 0)
		{
			PrintDriveTable(console, mediaDrives, includeIdColumn: true);
		}

		return await Task.FromResult(0);
	}

	private static void PrintDriveTable(
		IAnsiConsole console,
		List<BMTP3.Core4.Api.Models.DriveCatalogEntry> drives,
		bool includeIdColumn)
	{
		const int gap = 2;
		int nameWidth = Math.Max(drives.Max(d => d.Name?.Length ?? 0), 4) + gap;
		int typeWidth = 14;
		int rootWidth = Math.Max(drives.Max(d => d.RootPath?.Length ?? 0), 10) + gap;
		int sizeWidth = 12;
		int freeWidth = 12;

		console.WriteLine();
		if (includeIdColumn)
		{
			int idWidth = Math.Max(drives.Max(d => d.Id?.Length ?? 0), "Id".Length) + gap;
			int cNameWidth = Math.Max(nameWidth, "Name".Length + gap);
			console.WriteLine($"  {"Id".PadRight(idWidth)} {"Name".PadRight(cNameWidth)} {"Type".PadRight(typeWidth)} {"Root Path".PadRight(rootWidth)} {"Total Size".PadLeft(sizeWidth)} {"Free Space".PadLeft(freeWidth)}");
			console.WriteLine($"  {new string('-', idWidth + cNameWidth + typeWidth + rootWidth + sizeWidth + freeWidth + 5)}");
			foreach (var d in drives)
			{
				console.WriteLine($"  {d.Id.PadRight(idWidth)} {d.Name.PadRight(cNameWidth)} {d.SourceType.ToString().PadRight(typeWidth)} {d.RootPath.PadRight(rootWidth)} {FormatSize(d.TotalSize).PadLeft(sizeWidth)} {FormatSize(d.AvailableFreeSpace).PadLeft(freeWidth)}");
			}
		}
		else
		{
			int cNameWidth = Math.Max(nameWidth, "Id/Name".Length + gap);
			console.WriteLine($"  {"Id/Name".PadRight(cNameWidth)} {"Type".PadRight(typeWidth)} {"Root Path".PadRight(rootWidth)} {"Total Size".PadLeft(sizeWidth)} {"Free Space".PadLeft(freeWidth)}");
			console.WriteLine($"  {new string('-', cNameWidth + typeWidth + rootWidth + sizeWidth + freeWidth + 4)}");
			foreach (var d in drives)
			{
				console.WriteLine($"  {d.Name.PadRight(cNameWidth)} {d.SourceType.ToString().PadRight(typeWidth)} {d.RootPath.PadRight(rootWidth)} {FormatSize(d.TotalSize).PadLeft(sizeWidth)} {FormatSize(d.AvailableFreeSpace).PadLeft(freeWidth)}");
			}
		}
	}

	private static string FormatSize(long bytes)
	{
		if (bytes < 1024)
			return $"{bytes} B";
		if (bytes < 1024 * 1024)
			return $"{bytes / 1024.0:F1} KB";
		if (bytes < 1024L * 1024 * 1024)
			return $"{bytes / (1024.0 * 1024):F1} MB";

		return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";
	}
}
