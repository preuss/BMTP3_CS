using BMTP3.Consoles.ConsoleCommands.Core4;
using BMTP3.Consoles.Services;
using BMTP3.Core4.Api;
using BMTP3.Core4.Api.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.CommandLine;

namespace BMTP3.Consoles.ConsoleCommands;

public class BackupConsoleCommand4 : BaseConsoleCommand
{
	public BackupConsoleCommand4() : this(
		"backup4",
		"Perform backup using Core4 engine",
		new GlobalOptionsModel(),
		new BackupOptionsModel4()
	)
	{ }

	private BackupConsoleCommand4(
		string name,
		string description,
		GlobalOptionsModel globalOptionsModel,
		BackupOptionsModel4 backupOptionsModel
	) : base(name, description, globalOptionsModel, backupOptionsModel)
	{
		GlobalOptions = globalOptionsModel;
		BackupOptions = backupOptionsModel;
	}

	private GlobalOptionsModel GlobalOptions { get; }
	private BackupOptionsModel4 BackupOptions { get; }
	public IServiceProvider? ServiceProvider { get; init; }

	protected override async Task<int> DoExecuteAsync(
		ParseResult parseResult,
		CancellationToken cancellationToken
	)
	{
		ConsolesPrinter? consolePrinter = ServiceProvider.GetService<ConsolesPrinter>();
		consolePrinter?.PrintOptionsModel(GlobalOptions, BackupOptions);

		ILogger<BackupConsoleCommand4> logger = ServiceProvider.GetService<ILogger<BackupConsoleCommand4>>()
												?? ServiceProvider.GetService<ILoggerFactory>()
												?.CreateLogger<BackupConsoleCommand4>()
												?? NullLogger<BackupConsoleCommand4>.Instance;

		ValidateBackupOptions(BackupOptions);

		BackupPlan plan = BackupConsoleCommand4Helpers.BuildPlan(BackupOptions, parseResult);

		consolePrinter?.PrintStatus($"Starting Core4 backup: Name='{plan.Name}' SourceType={plan.SourceType} SourcePath='{plan.SourcePath}' Destination='{plan.Destination}'");
		logger.LogInformation("Starting Core4 backup: Name='{Name}' SourceType={SourceType} SourcePath='{SourcePath}' Destination='{Destination}'", plan.Name, plan.SourceType, plan.SourcePath, plan.Destination);

		IBackupEngine? engine = ServiceProvider.GetService<IBackupEngine>();
		if (engine == null)
		{
			logger.LogError("Core4 backup engine not configured in DI.");
			return 1;
		}

		Progress<BackupProgress> progress = new(p =>
		{
			consolePrinter?.PrintProgress(p);
			//DEBUG: silent logger — re-enable when debugging progress spam
			//logger.LogInformation("{Phase}: discovered={Discovered} succeeded={Succeeded} failed={Failed}", p.CurrentPhase, p.FilesDiscovered, p.FilesSucceeded, p.FilesFailed);
		});

		CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		ConsoleCancelEventHandler? cancelHandler = (s, e) =>
		{
			e.Cancel = true;
			logger.LogInformation("Cancellation requested, stopping Core4 backup...");
			linkedCts.Cancel();
		};
		Console.CancelKeyPress += cancelHandler;

		try
		{
			BackupResult result = await engine.RunAsync(plan, progress, linkedCts.Token);

			consolePrinter?.PrintResult(result);
			logger.LogInformation("Job '{JobName}' finished: {State}", result.Name, result.State);
			logger.LogInformation(
				"Items: {Total} Succeeded: {Succeeded} Failed: {Failed}",
				result.ItemResults.Count,
				result.ItemResults.Count(r => r.State == BMTP3.Core4.Api.Models.Enums.BackupResultItemState.Succeeded),
				result.ItemResults.Count(r => r.State == BMTP3.Core4.Api.Models.Enums.BackupResultItemState.Failed));

			if (result.FailureReason is not null)
			{
				logger.LogWarning("Failure reason: {Reason}", result.FailureReason);
			}

			return result.State == BMTP3.Core4.Api.Models.Enums.BackupResultState.Completed ? 0 : 1;
		}
		catch (OperationCanceledException)
		{
			consolePrinter?.PrintStatus("Core4 backup cancelled.");
			logger.LogInformation("Core4 backup cancelled.");
			return 2;
		}
		catch (Exception ex)
		{
			consolePrinter?.PrintError($"Unhandled error in Core4 backup: {ex.Message}");
			logger.LogError(ex, "Unhandled error in Core4 backup");
			return 1;
		}
		finally
		{
			Console.CancelKeyPress -= cancelHandler!;
		}
	}

	public Task<int> ExecuteAsyncForTests(ParseResult parseResult, CancellationToken cancellationToken)
	{
		return DoExecuteAsync(parseResult, cancellationToken);
	}

	private void ValidateBackupOptions(BackupOptionsModel4 backupOptions)
	{
		if (backupOptions.OutputStrategy == BMTP3.Core2.BackupNew.Api.Request.Enums.OutputStructureStrategy.CustomPathPattern
			&& string.IsNullOrWhiteSpace(backupOptions.CustomOutputFilePath))
		{
			throw new ArgumentException("--path-pattern is required when --output-structure is CustomPathPattern.");
		}

		if (backupOptions.RenameStrategy == BMTP3.Core2.BackupNew.Api.Request.Enums.RenameStrategy.CustomCollisionPathPattern
			&& string.IsNullOrWhiteSpace(backupOptions.CustomCollisionOutputFilePath))
		{
			throw new ArgumentException("--collision-pattern is required when --rename-strategy is CustomCollisionPathPattern.");
		}

		if ((backupOptions.Config == null || !backupOptions.Config.Exists)
			&& backupOptions.OutputDirectory == null)
		{
			throw new ArgumentException("--output is required when not using a config file.");
		}

		if ((backupOptions.Config == null || !backupOptions.Config.Exists)
			&& string.IsNullOrWhiteSpace(backupOptions.SourceDirectory))
		{
			throw new ArgumentException("--source-directory is required when not using a config file.");
		}
	}
}
