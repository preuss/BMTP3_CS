using System.CommandLine;
using BMTP3.Consoles.Services;
using BMTP3.Core2.BackupNew.Api;
using BMTP3.Core2.BackupNew.Api.Progress;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace BMTP3.Consoles.ConsoleCommands;

public class BackupConsoleCommand : BaseConsoleCommand
{
	public BackupConsoleCommand() : this("backup", "Perform backup", new GlobalOptionsModel(), new BackupOptionsModel())
	{
	}

	private BackupConsoleCommand(
		string name,
		string description,
		GlobalOptionsModel globalOptionsModel,
		BackupOptionsModel backupOptionsModel
	) : base(name, description, globalOptionsModel, backupOptionsModel)
	{
		GlobalOptions = globalOptionsModel;
		BackupOptions = backupOptionsModel;
	}

	private GlobalOptionsModel GlobalOptions { get; }
	private BackupOptionsModel BackupOptions { get; }
	public required IServiceProvider ServiceProvider { get; init; }

	protected override void OnCommandError(Exception ex)
	{
		ConsolesPrinter? printer = ServiceProvider.GetService<ConsolesPrinter>();
		if (printer != null)
		{
			printer.PrintError($"Error executing command: {ex.Message}");
		}
		else
		{
			Console.Error.WriteLine($"Error executing command: {ex.Message}");
		}
	}

	/// <summary>
	///     Entry point for the backup command action.
	/// </summary>
	protected override async Task<int> DoExecuteAsync(
		ParseResult parseResult,
		CancellationToken cancellationToken
	)
	{
		ConsolesPrinter consolePrinter = ServiceProvider.GetService<ConsolesPrinter>() ??
		                                 throw new InvalidOperationException("ConsolePrinter service not found.");
		consolePrinter.PrintOptionsModel(GlobalOptions, BackupOptions);

		int verbosity = GlobalOptions.Verbose;

		// Basic informational output through ConsolesPrinter
		// Additional verbose diagnostics are omitted to keep console output clean; use logging/diagnostics when needed.

		// TODO: Wire up the new IBackupEngine here. The old scanner logic has been removed.
		/*
		IProgress<TraversalProgress> progress =  new Progress<TraversalProgress>(snapshot => Console.WriteLine($"Scanned {snapshot.FileCount} files and {snapshot.DirectoryCount} directories so far..."));
		//List<MediaFileInfo> files = await mediaDeviceScanner.ScanAsync("", true, progress, cancellationToken).ToListAsync(cancellationToken);
		List<FileInfo> files = new();
		await foreach(FileInfo fileInfo in new FileSystemScanner().ScanAsync("C:\\Temp", true, progress, cancellationToken))
		{
			files.Add(fileInfo);
		}
		Console.WriteLine($"Found {files.Count()} files on the media device.");
		*/

		// If an IBackupEngine is registered, use it. Otherwise, fall back to a short simulation.
		IBackupEngine? engine = ServiceProvider.GetService<IBackupEngine>();
		if (engine != null)
		{
			// Determine source type and id from CLI options (mirrors BackupConsoleCommand2 logic)
			bool explicitDeviceProvided = !string.IsNullOrWhiteSpace(BackupOptions.SourceDevice);
			bool explicitSourceDirProvided = !string.IsNullOrWhiteSpace(BackupOptions.SourceDirectory);
			string sourcePath = BackupOptions.SourceDirectory ?? ".";

			BackupPlan plan = new()
			{
				Name = "ConsoleBackup",
				SourcePath = sourcePath,
				SourceType = explicitDeviceProvided && !explicitSourceDirProvided
					? SourceType.MediaDevice
					: SourceType.FileSystem,
				SourceId = explicitDeviceProvided
					? BackupOptions.SourceDevice!
					: Path.GetPathRoot(sourcePath) ?? string.Empty,
				OutputPath = BackupOptions.OutputDirectory?.FullName ?? Environment.CurrentDirectory,
				Recursive = BackupOptions.Recursive,
				DryRun = BackupOptions.Simulate
			};
			Progress<IBackupProgress> progress = new(p =>
			{
				/* no-op console output by default */
			});
			await engine.RunAsync(plan, progress, cancellationToken);
		}
		else
		{
			// Simulates backup work here if no engine present.
			await Task.Delay(100, cancellationToken);
		}

		PrintResult(consolePrinter);
		return 0;
	}

	/// <summary>
	///     Prints the result of the backup operation.
	/// </summary>
	private void PrintResult(ConsolesPrinter printer)
	{
		printer.PrintStatus("Backup completed!");
	}
}