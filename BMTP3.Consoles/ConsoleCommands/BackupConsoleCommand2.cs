using BMTP3.Consoles.Services;
using BMTP3.Core2.BackupNew.Api;
using BMTP3.Core2.BackupNew.Api.Progress;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Api.Response;
using BMTP3.Core2.BackupNew.Domain.Job;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.CommandLine;

namespace BMTP3.Consoles.ConsoleCommands;

public class BackupConsoleCommand2 : BaseConsoleCommand
{
	private GlobalOptionsModel GlobalOptions { get; }
	private BackupOptionsModel BackupOptions { get; }
	public required IServiceProvider ServiceProvider { get; init; }

	public BackupConsoleCommand2() : this("backup", "Perform backup", new GlobalOptionsModel(), new BackupOptionsModel())
	{
	}

	private BackupConsoleCommand2(
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
		ConsolesPrinter? consolePrinter = ServiceProvider.GetService<ConsolesPrinter>();
		consolePrinter?.PrintOptionsModel(GlobalOptions, BackupOptions);

		// Resolve logger from DI when available; fall back to a no-op logger so callers
		// (including tests) can capture structured logs instead of relying on Console.
		ILogger<BackupConsoleCommand2> logger = ServiceProvider.GetService<ILogger<BackupConsoleCommand2>>()
			?? ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger<BackupConsoleCommand2>()
			?? NullLogger<BackupConsoleCommand2>.Instance;

		ValidateBackupOptions(BackupOptions);
		BackupPlan plan = EngineArgumentBuilder(BackupOptions);


		// Friendly user-facing message
		consolePrinter?.PrintStatus($"Starting backup: Name='{plan.Name}' SourceType={plan.SourceType} SourcePath='{plan.SourcePath}' OutputPath='{plan.OutputPath}'");
		// Also log structured diagnostic information for tests/CI
		logger.LogInformation("Starting backup: Name='{Name}' SourceType={SourceType} SourcePath='{SourcePath}' OutputPath='{OutputPath}'", plan.Name, plan.SourceType, plan.SourcePath, plan.OutputPath);

		IBackupEngine? engine = ServiceProvider.GetService<IBackupEngine>();
		if(engine == null)
		{
			logger.LogError("Backup engine not configured in DI.");
			return 1;
		}

		Progress<IBackupProgress> progress = new(p =>
		{
			consolePrinter?.PrintProgress(p); // user-friendly progress
			logger.LogInformation("{Phase}: discovered={Discovered} succeeded={Succeeded} failed={Failed}", p.Phase, p.FilesDiscovered, p.FilesSucceeded, p.FilesFailed);
		});

		// Support Ctrl+C for interactive cancellation and link to provided token
		CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		ConsoleCancelEventHandler? cancelHandler = (s, e) =>
		{
			e.Cancel = true; // prevent process termination so we can cleanup
			logger.LogInformation("Cancellation requested, stopping backup...");
			linkedCts.Cancel();
		};
		Console.CancelKeyPress += cancelHandler;

		try
		{
			BackupJobResult result = await engine.RunAsync(plan, progress, linkedCts.Token);
			// Print friendly result for the user and log diagnostics
			consolePrinter?.PrintResult(result);
			logger.LogInformation("Job '{JobName}' finished: {Status}", result.JobName, result.Status);
			logger.LogInformation("Scanned: {Scanned} Copied: {Copied} Failed: {Failed} Skipped: {Skipped} Bytes: {Bytes}", result.TotalFilesScanned, result.FilesCopied, result.FilesFailed, result.FilesSkipped, result.TotalBytesCopied);
			if(result.GlobalErrors?.Count > 0)
			{
				logger.LogWarning("Global errors:");
				foreach(string e in result.GlobalErrors) logger.LogWarning(e);
			}

			return result.Status == BMTP3.Core2.BackupNew.Domain.Job.JobState.Completed ? 0 : 1;
		} catch(OperationCanceledException)
		{
			consolePrinter?.PrintStatus("Backup cancelled.");
			logger.LogInformation("Backup cancelled.");
			return 2;
		} catch(Exception ex)
		{
			// Friendly message for the user about failure
			consolePrinter?.PrintError($"Unhandled error running backup: {ex.Message}");
			// Log full exception to aid diagnostics in automated tests / CI
			logger.LogError(ex, "Unhandled error running backup");
			return 1;
		} finally
		{
			Console.CancelKeyPress -= cancelHandler!;
			//linkedCts.Dispose();
		}
	}

	// Public test helper to invoke the command logic directly from tests.
	public Task<int> ExecuteAsyncForTests(ParseResult parseResult, CancellationToken cancellationToken)
	{
		return DoExecuteAsync(parseResult, cancellationToken);
	}

	/// <summary>
	/// Programmatic helper that runs the engine for the given plan and returns the BackupJobResult.
	/// This is intended for tests and programmatic invocation where consumers need the structured result
	/// instead of an exit code.
	/// </summary>
	public async Task<BackupJobResult> TryRunAsync(BackupPlan plan, IProgress<IBackupProgress>? progress, CancellationToken ct)
	{
		ConsolesPrinter? consolePrinter = ServiceProvider.GetService<ConsolesPrinter>();
		ILogger<BackupConsoleCommand2>? logger = ServiceProvider.GetService<ILogger<BackupConsoleCommand2>>()
			?? ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger<BackupConsoleCommand2>();

		if(plan == null) throw new ArgumentNullException(nameof(plan));

		IBackupEngine? engine = ServiceProvider.GetService<IBackupEngine>();
		if(engine == null)
		{
			BackupJobResult fail = new() { JobName = plan.Name, StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow, Status = JobState.Failed };
			fail.GlobalErrors.Add("Backup engine not configured in DI.");
			logger?.LogError("Backup engine not configured in DI.");
			return fail;
		}

		try
		{
			BackupJobResult result = await engine.RunAsync(plan, progress ?? new Progress<IBackupProgress>(p => { }), ct);
			return result ?? new() { JobName = plan.Name, StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow, Status = JobState.Failed };
		} catch(OperationCanceledException)
		{
			logger?.LogInformation("Backup cancelled (TryRunAsync)");
			return new() { JobName = plan.Name, StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow, Status = JobState.Cancelled };
		} catch(Exception ex)
		{
			logger?.LogError(ex, "Unhandled exception during TryRunAsync");
			BackupJobResult r = new() { JobName = plan.Name, StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow, Status = JobState.Failed };
			r.GlobalErrors.Add(ex.Message);
			r.GlobalErrors.Add(ex.ToString());
			return r;
		}
	}

	/// <summary>
	/// Prints the result of the backup operation.
	/// </summary>
	private void PrintResult()
	{
		ConsolesPrinter? printer = ServiceProvider.GetService<ConsolesPrinter>();
		printer?.PrintStatus("Backup completed!");
	}

	/// <summary>
	/// Validates BackupOptionsModel for logical consistency.
	/// Throws exceptions with clear error messages for invalid combinations.
	/// </summary>
	private void ValidateBackupOptions(BackupOptionsModel backupOptions)
	{
		if(backupOptions.OutputStrategy == OutputStructureStrategy.CustomPathPattern
			&& string.IsNullOrWhiteSpace(backupOptions.CustomOutputFilePath)
		)
		{
			throw new ArgumentException("--path-pattern is required when --output-structure is CustomPathPattern.");
		}

		if(backupOptions.RenameStrategy == RenameStrategy.CustomCollisionPathPattern
			&& string.IsNullOrWhiteSpace(backupOptions.CustomCollisionOutputFilePath)
		)
		{
			throw new ArgumentException("--collision-pattern is required when --rename-strategy is CustomCollisionPathPattern.");
		}

		if((backupOptions.Config == null || !backupOptions.Config.Exists)
			&& string.IsNullOrWhiteSpace(backupOptions.SourceDirectory))
		{
			throw new ArgumentException("--source-directory is required when not using a config file.");
		}

		if((backupOptions.Config == null || !backupOptions.Config.Exists)
			&& backupOptions.OutputDirectory == null)
		{
			throw new ArgumentException("--output is required when not using a config file.");
		}

		if(backupOptions.Delay < 0)
		{
			throw new ArgumentException("--delay must be >= 0.");
		}

		if(backupOptions.VerificationRetryCount < 1)
		{
			throw new ArgumentException("--verify-retry-count must be >= 1.");
		}

		if(backupOptions.VerificationRetryDelayMs < 0)
		{
			throw new ArgumentException("--verify-retry-delay must be >= 0.");
		}

		if(backupOptions.VerificationTimeoutMs < 0)
		{
			throw new ArgumentException("--verify-timeout must be >= 0.");
		}
	}

	/// <summary>
	/// Builds a BackupPlan from the CLI options.
	/// Handles defaults that depend on combinations of options.
	/// </summary>
	private BackupPlan EngineArgumentBuilder(BackupOptionsModel backupOptions)
	{
		BackupPlan plan = new()
		{
			Name = backupOptions.Name
				?? (backupOptions.Config != null
					? Path.GetFileNameWithoutExtension(backupOptions.Config.Name)
					: null)
				?? backupOptions.SourceDevice
				?? (backupOptions.OutputDirectory != null
					? backupOptions.OutputDirectory.Name
					: null)
				?? "backup",
			SourcePath = backupOptions.SourceDirectory!,
			OutputPath = backupOptions.OutputDirectory!.FullName,
			Recursive = backupOptions.Recursive,
			DryRun = backupOptions.Simulate,
			IncludePatterns = backupOptions.IncludePatterns,
			ExcludePatterns = backupOptions.ExcludePatterns,
			OutputStrategy = backupOptions.OutputStrategy,
			CollisionResolution = backupOptions.CollisionResolutionType,
			ComparisonType = backupOptions.CollisionComparison,
			RenameStrategy = backupOptions.RenameStrategy,
			SidecarFormat = backupOptions.SidecarFormat,
			BackupIndexType = backupOptions.BackupIndexType,
			DelayMs = backupOptions.Delay,
			CustomOutputPathPattern = backupOptions.CustomOutputFilePath,
			CustomCollisionPathPattern = backupOptions.CustomCollisionOutputFilePath,
			SourceType = !string.IsNullOrWhiteSpace(backupOptions.SourceDevice) ? SourceType.MediaDevice : SourceType.FileSystem,
			PostWriteVerification = backupOptions.PostWriteVerification,
			VerificationRetryCount = backupOptions.VerificationRetryCount,
			VerificationRetryDelayMs = backupOptions.VerificationRetryDelayMs,
			VerificationDeleteOnFailure = backupOptions.VerificationDeleteOnFailure,
			VerificationTimeoutMs = backupOptions.VerificationTimeoutMs
		};

		if(!string.IsNullOrWhiteSpace(backupOptions.SourceDevice))
		{
			plan.SourceId = backupOptions.SourceDevice!;
		}
		else
		{
			plan.SourceId = Path.GetPathRoot(Path.GetFullPath(plan.SourcePath ?? ".")) ?? string.Empty;
		}

		return plan;
	}
}
