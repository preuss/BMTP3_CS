using BMTP3.Consoles.Services;
using BMTP3.Core2.BackupNew.Api;
using BMTP3.Core2.BackupNew.Api.Progress;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;

namespace BMTP3.Consoles.ConsoleCommands;
public class BackupTestConsoleCommand : BaseConsoleCommand
{
	private GlobalOptionsModel GlobalOptions { get; }
	private BackupOptionsModel BackupOptions { get; }
	public required IServiceProvider ServiceProvider { get; init; }

	public BackupTestConsoleCommand() : this("backupTest", "Perform backuptest", new GlobalOptionsModel(), new BackupOptionsModel())
	{
	}

	private BackupTestConsoleCommand(
		string name,
		string description,
		GlobalOptionsModel globalOptionsModel,
		BackupOptionsModel backupOptionsModel
	) : base(name, description, globalOptionsModel, backupOptionsModel)
	{
		GlobalOptions = globalOptionsModel;
		BackupOptions = backupOptionsModel;
	}

	protected override async Task<int> DoExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
	{
		// Print options for diagnostics
		var printer = ServiceProvider.GetService<ConsolesPrinter>();
		printer?.PrintOptionsModel(GlobalOptions, BackupOptions);

		// Resolve engine
		var engine = ServiceProvider.GetRequiredService<IBackupEngine>();

		// Map CLI options to BackupPlan
		var plan = new BackupPlan
		{
			Name = BackupOptions.Config?.Name ?? "console-backup",
			SourceType = string.IsNullOrWhiteSpace(BackupOptions.SourceDevice) ? SourceType.FileSystem : SourceType.MediaDevice,
			SourcePath = BackupOptions.SourceDirectory ?? ".",
			SourceId = BackupOptions.SourceDevice ?? string.Empty,
			OutputPath = BackupOptions.OutputDirectory?.FullName ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "BMTP3_Backups"),
			Recursive = BackupOptions.Recursive
		};

		Console.WriteLine($"Starting backup: Name='{plan.Name}' SourceType={plan.SourceType} SourcePath='{plan.SourcePath}' OutputPath='{plan.OutputPath}'");

		// Ensure output path is present (JobValidator will attempt create, but be explicit)
		try
		{
			if(!string.IsNullOrWhiteSpace(plan.OutputPath) && !Directory.Exists(plan.OutputPath))
				Directory.CreateDirectory(plan.OutputPath);
		} catch(Exception ex)
		{
			Console.WriteLine($"Failed to prepare output directory '{plan.OutputPath}': {ex.Message}");
			return 1;
		}

		// Simple console progress reporter
		var progress = new Progress<IBackupProgress>(p =>
		{
			Console.WriteLine($"{p.Phase}: discovered={p.FilesDiscovered} succeeded={p.FilesSucceeded} failed={p.FilesFailed}");
		});

		try
		{
			var result = await engine.RunAsync(plan, progress, cancellationToken);

			Console.WriteLine($"Job '{result.JobName}' finished: {result.Status}");
			Console.WriteLine($"Scanned: {result.TotalFilesScanned} Copied: {result.FilesCopied} Failed: {result.FilesFailed} Skipped: {result.FilesSkipped} Bytes: {result.TotalBytesCopied}");
			if(result.GlobalErrors?.Count > 0)
			{
				Console.WriteLine("Global errors:");
				foreach(var e in result.GlobalErrors) Console.WriteLine($"  - {e}");
			}

			return result.Status == BMTP3.Core2.BackupNew.Domain.Job.JobState.Completed ? 0 : 1;
		} catch(OperationCanceledException)
		{
			Console.WriteLine("Backup cancelled.");
			return 2;
		} catch(Exception ex)
		{
			Console.WriteLine($"Unhandled error running backup: {ex.Message}");
			return 1;
		}
	}
}
