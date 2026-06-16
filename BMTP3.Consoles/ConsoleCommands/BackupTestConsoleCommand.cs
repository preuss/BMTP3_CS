using BMTP3.Consoles.Services;
using BMTP3.Core2.BackupNew.Api;
using BMTP3.Core2.BackupNew.Api.Progress;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Api.Response;
using BMTP3.Core2.BackupNew.Domain.Job;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;

namespace BMTP3.Consoles.ConsoleCommands;

public class BackupTestConsoleCommand : BaseConsoleCommand
{
	public BackupTestConsoleCommand() : this("backupTest", "Perform test-backup", new GlobalOptionsModel(),
		new BackupOptionsModel())
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

	private GlobalOptionsModel GlobalOptions { get; }
	private BackupOptionsModel BackupOptions { get; }
	public required IServiceProvider ServiceProvider { get; init; }

	protected override void OnCommandError(Exception ex)
	{
		ConsolesPrinter2? printer = ServiceProvider.GetService<ConsolesPrinter2>();
		if (printer != null)
		{
			printer.PrintError($"Error executing command: {ex.Message}");
		}
		else
		{
			Console.Error.WriteLine($"Error executing command: {ex.Message}");
		}
	}

	protected override async Task<int> DoExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
	{
		ConsolesPrinter2 printer = ServiceProvider.GetRequiredService<ConsolesPrinter2>();
		printer.PrintOptionsModel(GlobalOptions, BackupOptions);

		IBackupEngine engine = ServiceProvider.GetRequiredService<IBackupEngine>();

		BackupPlan plan = new()
		{
			Name = BackupOptions.Config?.Name ?? "console-backup",
			SourceType = string.IsNullOrWhiteSpace(BackupOptions.SourceDevice)
				? SourceType.FileSystem
				: SourceType.MediaDevice,
			SourcePath = BackupOptions.SourceDirectory ?? ".",
			SourceId = BackupOptions.SourceDevice ?? string.Empty,
			OutputPath = BackupOptions.OutputDirectory?.FullName ??
						 Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
							 "BMTP3_Backups"),
			Recursive = BackupOptions.Recursive
		};

		printer.PrintStatus(
			$"Starting backup: Name='{plan.Name}' SourceType={plan.SourceType} SourcePath='{plan.SourcePath}' OutputPath='{plan.OutputPath}'");

		try
		{
			if (!string.IsNullOrWhiteSpace(plan.OutputPath) && !Directory.Exists(plan.OutputPath))
			{
				Directory.CreateDirectory(plan.OutputPath);
			}
		}
		catch (Exception ex)
		{
			printer.PrintError($"Failed to prepare output directory '{plan.OutputPath}': {ex.Message}");
			return 1;
		}

		Progress<IBackupProgress> progress = new(p => printer.PrintProgress(p));

		try
		{
			BackupJobResult result = await engine.RunAsync(plan, progress, cancellationToken);
			printer.PrintResult(result);
			return result.Status == JobState.Completed ? 0 : 1;
		}
		catch (OperationCanceledException)
		{
			printer.PrintStatus("Backup cancelled.");
			return 2;
		}
		catch (Exception ex)
		{
			printer.PrintError($"Unhandled error running backup: {ex.Message}");
			return 1;
		}
	}
}