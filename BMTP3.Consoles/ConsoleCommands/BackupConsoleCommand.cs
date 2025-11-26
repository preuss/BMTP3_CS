using BMTP3.Consoles.Services;
using BMTP3.Core.Handlers;
using BMTP3.Core2.BackupNew.Events;
using BMTP3.Core2.BackupNew.Reader;
using MediaDevices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;

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

		//var backupMaster = ServiceProvider.GetService<BackupMaster>();
		//BackupMaster bm;

		IEnumerable<MediaDevice> privateDevices = MediaDevice.GetPrivateDevices();
		IEnumerable<MediaDevice> publicDevices = MediaDevice.GetDevices();
		MediaDevice device = publicDevices.FirstOrDefault() ?? throw new InvalidOperationException("No media devices found.");
		device.Connect();
		MediaDeviceScanner mediaDeviceScanner = new MediaDeviceScanner(device);
		TraversalProgressCounter progressCounter = new TraversalProgressCounter();
		progressCounter.CombinedCountChanged += (sender, snapshot) =>
		{
			Console.WriteLine($"Scanned {snapshot.FileCount} files and {snapshot.DirectoryCount} directories so far...");
		};
		IEnumerable<MediaDevices.MediaFileInfo> files = mediaDeviceScanner.TraverseFiles(progress: progressCounter);
		Console.WriteLine($"Found {files.Count()} files on the media device.");
		device.Disconnect();

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