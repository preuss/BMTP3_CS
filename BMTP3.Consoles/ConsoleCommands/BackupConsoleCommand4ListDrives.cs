using System.CommandLine;
using BMTP3.Consoles.Services;
using BMTP3.Core4.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BMTP3.Consoles.ConsoleCommands;

public class BackupConsoleCommand4ListDrives : BaseConsoleCommand
{
	public BackupConsoleCommand4ListDrives() : base(
		"list-sources",
		"List available backup sources (drives and MTP devices)",
		new GlobalOptionsModel()
	)
	{
	}

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

		IReadOnlyList<Core4.Api.Models.DriveCatalogEntry> drives = catalogService.ListDrives();

		if (drives.Count == 0)
		{
			logger.LogInformation("No backup sources found.");
			return 0;
		}

		int nameWidth = Math.Max(drives.Max(d => d.Name?.Length ?? 0), 4) + 2;
		int typeWidth = 14;
		int sizeWidth = 12;
		int freeWidth = 12;

		Console.Out.WriteLine("Available backup sources:");
		Console.Out.WriteLine(
			"  {0}{1}{2}{3}{4}",
			"Name".PadRight(nameWidth),
			"Type".PadRight(typeWidth),
			"Total Size".PadLeft(sizeWidth),
			"Free Space".PadLeft(freeWidth),
			"Root Path"
		);
		Console.Out.WriteLine(
			"  {0}",
			new string('-', nameWidth + typeWidth + sizeWidth + freeWidth + 30)
		);

		foreach (Core4.Api.Models.DriveCatalogEntry drive in drives)
		{
			Console.Out.WriteLine(
				"  {0}{1}{2}{3}{4}",
				(drive.Name ?? drive.Id).PadRight(nameWidth),
				drive.SourceType.ToString().PadRight(typeWidth),
				FormatSize(drive.TotalSize).PadLeft(sizeWidth),
				FormatSize(drive.AvailableFreeSpace).PadLeft(freeWidth),
				drive.RootPath
			);
		}

		return await Task.FromResult(0);
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
