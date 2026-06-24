using BMTP3.Consoles.ConsoleCommands.Core4;
using BMTP3.Consoles.Progress;
using BMTP3.Consoles.Services;
using BMTP3.Core4.Api;
using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Api.Models.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.CommandLine;
using System.CommandLine.Parsing;

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
	public required IServiceProvider ServiceProvider { get; init; }

	protected override async Task<int> DoExecuteAsync(
		ParseResult parseResult,
		CancellationToken cancellationToken
	)
	{
		ArgumentNullException.ThrowIfNull(ServiceProvider);

		IAnsiConsole ansiConsole = ServiceProvider.GetRequiredService<IAnsiConsole>();

		ConsolesPrinter4 consolePrinter = new(ansiConsole);
		ILogger<BackupConsoleCommand4> logger = ServiceProvider.GetRequiredService<ILogger<BackupConsoleCommand4>>();
		IBackupEngine engine = ServiceProvider.GetRequiredService<IBackupEngine>();

		BackupProgressDisplay display = new(ansiConsole);

		//consolePrinter.PrintOptionsModel(GlobalOptions, BackupOptions);

		BackupPlan plan = BackupConsoleCommand4Helpers.BuildPlan(BackupOptions, parseResult);

		PrintAndLogStart(consolePrinter, logger, plan);

		try
		{
			BackupResult result = await RunBackupWithProgressAsync(
				display, engine, plan, cancellationToken);
			consolePrinter.PrintResult(result);
			LogResult(logger, result);

			return result.State == BackupResultState.Completed ? 0 : 1;
		}
		catch (OperationCanceledException)
		{
			consolePrinter.PrintStatus("Core4 backup cancelled.");
			logger.LogInformation("Core4 backup cancelled.");
			return 2;
		}
		catch (Exception ex)
		{
			consolePrinter.PrintError($"Unhandled error in Core4 backup: {ex.Message}");
			logger.LogError(ex, "Unhandled error in Core4 backup");
			return 1;
		}
	}

	internal Task<int> ExecuteAsyncForTests(ParseResult parseResult, CancellationToken cancellationToken)
	{
		return DoExecuteAsync(parseResult, cancellationToken);
	}

	private static Task<BackupResult> RunBackupWithProgressAsync(
		BackupProgressDisplay display,
		IBackupEngine engine,
		BackupPlan plan,
		CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(display);
		ArgumentNullException.ThrowIfNull(engine);
		ArgumentNullException.ThrowIfNull(plan);

		return display.RunAsync(
			plan.Name,
			progress => engine.RunAsync(plan, progress, cancellationToken));
	}

	private static void PrintAndLogStart(
		ConsolesPrinter4 consolePrinter,
		ILogger<BackupConsoleCommand4> logger,
		BackupPlan plan)
	{
		consolePrinter.PrintStatus(
			$"Starting Core4 backup: Name='{plan.Name}' SourceType={plan.SourceType} SourcePath='{plan.SourcePath}' Destination='{plan.Destination}'");

		logger.LogInformation(
			"Starting Core4 backup: Name='{Name}' SourceType={SourceType} SourcePath='{SourcePath}' Destination='{Destination}'",
			plan.Name,
			plan.SourceType,
			plan.SourcePath,
			plan.Destination);
	}

	private static void LogResult(
		ILogger<BackupConsoleCommand4> logger,
		BackupResult result)
	{
		BackupResultCounts counts = result.Counts;

		logger.LogInformation("Job '{JobName}' finished: {State}", result.Name, result.State);

		logger.LogInformation(
			"Items: {Total} Succeeded: {Succeeded} Failed: {Failed} Skipped: {Skipped}",
			counts.Total,
			counts.Succeeded,
			counts.Failed,
			counts.Skipped);

		if (result.FailureReason is not null)
		{
			logger.LogWarning("Failure reason: {Reason}", result.FailureReason);
		}
	}

	protected override void DoValidateCommandOptions(CommandResult result)
	{
		bool hasConfig = result.GetResult(BackupOptionsModel4.ConfigOption) is { Tokens: { Count: > 0 } };

		if(!hasConfig)
		{
			if(result.GetResult(BackupOptionsModel4.SourceTypeOption) is not { Tokens: { Count: > 0 } })
				result.AddError("--source-type is required when --config is not provided.");

			if(result.GetResult(BackupOptionsModel4.SourcePathOption) is not { Tokens: { Count: > 0 } })
				result.AddError("--source-path is required when --config is not provided.");

			if(result.GetResult(BackupOptionsModel4.OutputDirectoryOption) is not { Tokens: { Count: > 0 } })
				result.AddError("--output is required when --config is not provided.");
		}

		if(result.GetResult(BackupOptionsModel4.OutputStrategyOption) is { } strategyResult
		   && strategyResult.GetValueOrDefault<OutputStructureStrategy>() is OutputStructureStrategy strategy
		   && strategy == OutputStructureStrategy.CustomPathPattern
		   && result.GetResult(BackupOptionsModel4.CustomOutputFilePathOption) is not { Tokens: { Count: > 0 } })
		{
			result.AddError("--path-pattern is required when --output-structure is CustomPathPattern.");
		}

		if(result.GetResult(BackupOptionsModel4.RenameStrategyOption) is { } renameResult
		   && renameResult.GetValueOrDefault<RenameStrategy>() is RenameStrategy renameStrategy
		   && renameStrategy == RenameStrategy.CustomPattern
		   && result.GetResult(BackupOptionsModel4.CustomCollisionOutputFilePathOption) is not { Tokens: { Count: > 0 } })
		{
			result.AddError("--collision-pattern is required when --rename-strategy is CustomPattern.");
		}
	}
}