using BMTP3.Core4.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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

		int nameWidth = Math.Max(drives.Max(d => d.Name?.Length ?? 0), 4) + 2;
		int idWidth = Math.Max(drives.Max(d => d.Id?.Length ?? 0), 2) + 2;
		int typeWidth = 14;
		int rootWidth = Math.Max(drives.Max(d => d.RootPath?.Length ?? 0), 10) + 2;
		int sizeWidth = 12;
		int freeWidth = 12;

		List<BMTP3.Core4.Api.Models.DriveCatalogEntry> fileSystemDrives = drives.Where(d => d.SourceType == BMTP3.Core4.Api.Models.Enums.BackupSourceType.FileSystem).ToList();
		List<BMTP3.Core4.Api.Models.DriveCatalogEntry> mediaDrives = drives.Where(d => d.SourceType == BMTP3.Core4.Api.Models.Enums.BackupSourceType.MediaDevice).ToList();

		if (fileSystemDrives.Count > 0)
		{
			PrintDriveTable(fileSystemDrives, nameWidth, typeWidth, rootWidth, sizeWidth, freeWidth, includeIdColumn: false);
		}

		if (mediaDrives.Count > 0)
		{
			PrintDriveTable(mediaDrives, nameWidth, typeWidth, rootWidth, sizeWidth, freeWidth, includeIdColumn: true);
		}

		return await Task.FromResult(0);
	}

	private static void PrintDriveTable(
		List<BMTP3.Core4.Api.Models.DriveCatalogEntry> drives,
		int nameWidth, int typeWidth, int rootWidth, int sizeWidth, int freeWidth,
		bool includeIdColumn)
	{
		const int gap = 2;
		Console.Out.WriteLine();
		if (includeIdColumn)
		{
			int idWidth = Math.Max(drives.Max(d => d.Id?.Length ?? 0), "Id".Length) + gap;
			int cNameWidth = Math.Max(nameWidth, "Name".Length + gap);
			int tableWidth = idWidth + cNameWidth + typeWidth + rootWidth + sizeWidth + freeWidth + 5;
			Console.Out.WriteLine("  {0} {1} {2} {3} {4} {5}",
				"Id".PadRight(idWidth),
				"Name".PadRight(cNameWidth),
				"Type".PadRight(typeWidth),
				"Root Path".PadRight(rootWidth),
				"Total Size".PadLeft(sizeWidth),
				"Free Space".PadLeft(freeWidth));
			Console.Out.WriteLine("  {0}", new string('-', tableWidth));
			foreach (var d in drives)
			{
				Console.Out.WriteLine("  {0} {1} {2} {3} {4} {5}",
					d.Id.PadRight(idWidth),
					d.Name.PadRight(cNameWidth),
					d.SourceType.ToString().PadRight(typeWidth),
					d.RootPath.PadRight(rootWidth),
					FormatSize(d.TotalSize).PadLeft(sizeWidth),
					FormatSize(d.AvailableFreeSpace).PadLeft(freeWidth));
			}
		}
		else
		{
			int cNameWidth = Math.Max(nameWidth, "Id/Name".Length + gap);
			int tableWidth = cNameWidth + typeWidth + rootWidth + sizeWidth + freeWidth + 4;
			Console.Out.WriteLine("  {0} {1} {2} {3} {4}",
				"Id/Name".PadRight(cNameWidth),
				"Type".PadRight(typeWidth),
				"Root Path".PadRight(rootWidth),
				"Total Size".PadLeft(sizeWidth),
				"Free Space".PadLeft(freeWidth));
			Console.Out.WriteLine("  {0}", new string('-', tableWidth));
			foreach (var d in drives)
			{
				Console.Out.WriteLine("  {0} {1} {2} {3} {4}",
					d.Name.PadRight(cNameWidth),
					d.SourceType.ToString().PadRight(typeWidth),
					d.RootPath.PadRight(rootWidth),
					FormatSize(d.TotalSize).PadLeft(sizeWidth),
					FormatSize(d.AvailableFreeSpace).PadLeft(freeWidth));
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
