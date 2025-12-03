using BMTP3.Consoles.Services;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;

namespace BMTP3.Consoles.ConsoleCommands;

public class BackupConsoleCommand : BaseConsoleCommand
{
	private GlobalOptionsModel GlobalOptions { get; }
	private BackupOptionsModel BackupOptions { get; }
	public required IServiceProvider ServiceProvider { get; init; }

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

	/// <summary>
	/// Entry point for the backup command action.
	/// </summary>
	protected override async Task<int> DoExecuteAsync(
		ParseResult parseResult,
		CancellationToken cancellationToken
	)
	{
		ConsolesPrinter consolePrinter = ServiceProvider.GetService<ConsolesPrinter>() ?? throw new InvalidOperationException("ConsolePrinter service not found.");
		consolePrinter.PrintOptionsModel(GlobalOptions, BackupOptions);

		int verbosity = GlobalOptions.Verbose;

		Console.WriteLine("Verbose Level: " + verbosity);
		Console.WriteLine("Delay: " + BackupOptions.Delay);
		Console.WriteLine("Simulate: " + BackupOptions.Simulate);
		Console.WriteLine("Simulate Min: " + BackupOptionsModel.SimulateOption.Arity.MinimumNumberOfValues);
		Console.WriteLine("Simulate Max: " + BackupOptionsModel.SimulateOption.Arity.MaximumNumberOfValues);
		Console.WriteLine("Config: " + BackupOptions.Config);
		Console.WriteLine("Config Exists: " + BackupOptions.Config?.Exists);

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

		// Simulates backup work here.
		await Task.Delay(100, cancellationToken);
		// Add your actual backup logic here

		PrintResult();
		return 0;
	}

	/// <summary>
	/// Prints the result of the backup operation.
	/// </summary>
	private void PrintResult()
	{
		Console.WriteLine("Backup completed!");
	}
}