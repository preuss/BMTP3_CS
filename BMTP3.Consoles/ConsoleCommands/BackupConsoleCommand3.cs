using BMTP3.Consoles.Services;
using BMTP3.Core3;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.CommandLine;

namespace BMTP3.Consoles.ConsoleCommands;

/// <summary>
/// Console command for running Core3 (BackupEngineSequential) backup operations.
/// Mirrors BackupConsoleCommand2 but targets the sequential/simpler Core3 implementation.
/// </summary>
public class BackupConsoleCommand3 : BaseConsoleCommand
{
	public BackupConsoleCommand3() : this(
		"backup3",
		"Perform backup using Core3 (sequential engine)",
		new GlobalOptionsModel(),
		new BackupOptionsModel()
	)
	{
	}

	private BackupConsoleCommand3(
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
	public IServiceProvider? ServiceProvider { get; init; }

	protected override async Task<int> DoExecuteAsync(
		ParseResult parseResult,
		CancellationToken cancellationToken
	)
	{
		ConsolesPrinter3? consolePrinter = ServiceProvider.GetService<ConsolesPrinter3>();
		consolePrinter?.PrintOptionsModel(GlobalOptions, BackupOptions);

		ILogger<BackupConsoleCommand3> logger = ServiceProvider.GetService<ILogger<BackupConsoleCommand3>>()
													?? ServiceProvider.GetService<ILoggerFactory>()
													?.CreateLogger<BackupConsoleCommand3>()
													?? NullLogger<BackupConsoleCommand3>.Instance;

		ValidateBackupOptions(BackupOptions);

		// Build Core3-compatible BackupPlan
		BackupPlan plan = BuildCore3Plan(BackupOptions, parseResult);

		consolePrinter?.PrintStatus(
			$"Starting Core3 backup: Source='{plan.Source}' Destination='{plan.Destination}'"
		);
		logger.LogInformation(
			"Starting Core3 backup: Source='{Source}' Destination='{Destination}'",
			plan.Source, plan.Destination
		);

		IBackupEngine? engine = ServiceProvider.GetService<IBackupEngine>();
		if (engine == null)
		{
			logger.LogError("Core3 backup engine not configured in DI.");
			return 1;
		}

		Progress<BMTP3.Core3.IBackupProgress> progress = new(p =>
		{
			consolePrinter?.PrintProgress(p);
			logger.LogInformation(
				"{Phase}: file={CurrentFile} processed={FilesProcessed}/{FilesTotal}",
				p.Phase, p.CurrentFile, p.FilesProcessed, p.FilesTotal
			);
		});

		CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		ConsoleCancelEventHandler? cancelHandler = (s, e) =>
		{
			e.Cancel = true;
			logger.LogInformation("Cancellation requested, stopping Core3 backup...");
			linkedCts.Cancel();
		};
		Console.CancelKeyPress += cancelHandler;

		try
		{
			BackupJobResult result = await engine.RunAsync(plan, progress, linkedCts.Token);

			consolePrinter?.PrintStatus(
				$"Core3 backup completed: {result.SuccessfulItems}/{result.TotalItems} items, " +
				$"Duration: {result.Duration.TotalSeconds:F2}s"
			);
			logger.LogInformation(
				"Core3 backup result: Success={Success} Total={Total} Successful={Successful} " +
				"Failed={Failed} Duration={Duration}ms",
				result.Success, result.TotalItems, result.SuccessfulItems, result.FailedItems,
				result.Duration.TotalMilliseconds
			);

			if (result.Errors?.Count > 0)
			{
				logger.LogWarning("Core3 backup errors:");
				foreach (BackupError error in result.Errors)
				{
					logger.LogWarning("  {ItemName}: {Message}", error.ItemName, error.Message);
				}
			}

			return result.Success ? 0 : 1;
		}
		catch (OperationCanceledException)
		{
			consolePrinter?.PrintStatus("Core3 backup cancelled.");
			logger.LogInformation("Core3 backup cancelled.");
			return 2;
		}
		catch (Exception ex)
		{
			consolePrinter?.PrintError($"Unhandled error in Core3 backup: {ex.Message}");
			logger.LogError(ex, "Unhandled error in Core3 backup");
			return 1;
		}
		finally
		{
			Console.CancelKeyPress -= cancelHandler!;
		}
	}

	/// <summary>
	/// Builds a Core3-compatible BackupPlan from CLI options.
	/// Core3 has a simpler model (Source, Destination, DryRun, Collision, HashTypes).
	/// </summary>
	private BackupPlan BuildCore3Plan(BackupOptionsModel backupOptions, ParseResult parseResult)
	{
		// Determine source path
		string sourcePath = backupOptions.SourceDirectory
			?? throw new ArgumentException("--source-directory is required");

		// Determine destination path
		string destPath = backupOptions.OutputDirectory?.FullName
			?? throw new ArgumentException("--output is required");

		// Build Core3 BackupPlan
		return new BackupPlan
		{
			Source = Path.GetFullPath(sourcePath),
			Destination = Path.GetFullPath(destPath),
			DryRun = backupOptions.Simulate,
			Collision = MapCollisionStrategy(backupOptions.CollisionResolutionType),
			HashTypes = new List<HashType>
			{
				HashType.SHA2_256,
				HashType.SHA3_256_FIPS202,
				HashType.BLAKE3_256
			}
		};
	}

	/// <summary>
	/// Maps Core2 CollisionResolutionType to Core3 CollisionStrategy.
	/// </summary>
	private CollisionStrategy MapCollisionStrategy(
		BMTP3.Core2.BackupNew.Api.Request.Enums.CollisionResolutionType core2Type
	)
	{
		return core2Type switch
		{
			BMTP3.Core2.BackupNew.Api.Request.Enums.CollisionResolutionType.Rename =>
				CollisionStrategy.Rename,
			BMTP3.Core2.BackupNew.Api.Request.Enums.CollisionResolutionType.Skip =>
				CollisionStrategy.Skip,
			BMTP3.Core2.BackupNew.Api.Request.Enums.CollisionResolutionType.Overwrite =>
				CollisionStrategy.Overwrite,
			_ => CollisionStrategy.Rename
		};
	}

	private void ValidateBackupOptions(BackupOptionsModel backupOptions)
	{
		if (backupOptions.OutputDirectory == null)
		{
			throw new ArgumentException("--output is required for Core3 backup.");
		}

		if (string.IsNullOrWhiteSpace(backupOptions.SourceDirectory))
		{
			throw new ArgumentException("--source-directory is required for Core3 backup.");
		}
	}
}
