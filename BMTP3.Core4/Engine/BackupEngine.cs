using BMTP3.Core4.Api;
using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Engine.Runner;
using BMTP3.Core4.Engine.Session;
using BMTP3.Core4.Engine.Validation;
using BMTP3.Core4.Models;
using BMTP3.Core4.Models.Enums;
using BMTP3.Core4.Scanner;
using BMTP3.Core4.State;
using BMTP3.Core4.Traversal;

namespace BMTP3.Core4.Engine;

/// <summary>
/// Orchestrates the execution of a backup job.
/// This implementation defines the ordered steps of a backup run.
/// Each step is initially expressed as a comment and will later
/// be replaced by concrete implementation calls.
/// </summary>
public sealed class BackupEngine : IBackupEngine
{
	private readonly IBackupScanner _scanner;
	private readonly ISourceTraversalFactory _sourceTraversalFactory;
	private readonly IBackupRunnerFactory _backupRunnerFactory;
	private readonly ISummaryStore _summaryStore;

	internal BackupEngine(
		IBackupScanner scanner,
		ISourceTraversalFactory sourceTraversalFactory,
		IBackupRunnerFactory backupRunnerFactory,
		ISummaryStore summaryStore
	)
	{
		_scanner = scanner;
		_sourceTraversalFactory = sourceTraversalFactory;
		_backupRunnerFactory = backupRunnerFactory;
		_summaryStore = summaryStore;
	}

	public async Task<BackupResult> RunAsync(
		BackupPlan plan,
		IProgress<BackupProgress>? progress,
		CancellationToken cancellationToken
	)
	{
		// ------------------------------------------------------------
		// 1. Validate backup plan
		//    - Ensure required fields are present
		//    - Ensure source and destination are not the same
		//    - Fail fast on invalid configuration
		// ------------------------------------------------------------
		BackupPlanValidator.Validate(plan);

		// ------------------------------------------------------------
		// 2. Initialize backup state
		//    - Create internal state objects
		//    - Initialize progress tracking
		//    - Set phase = Starting
		// ------------------------------------------------------------

		BackupProgress _currentProgress = new()
		{
			SourcePath = plan.SourcePath,
			DestinationPath = plan.Destination,
			CurrentPhase = BackupProgressPhase.Starting,
		};
		progress?.Report(_currentProgress);

		IBackupRecordRepository repository = new BackupMemoryRecordRepository();

		BackupSessionKey sessionKey = BackupSessionKeyFactory.Create(plan);

		// ------------------------------------------------------------
		// 3. Open source traversal (filesystem or media device)
		//    - Establish access to source via ISourceTraversal
		//    - Fail if source is not accessible
		// ------------------------------------------------------------
		SourceTraversalFactoryCreateRequest sourceTraversalFactoryCreateRequest = new()
		{
			SourceType = plan.SourceType,
			SourcePath = plan.SourcePath,
		};

		await using ISourceTraversal traversal = _sourceTraversalFactory.Create(sourceTraversalFactoryCreateRequest);

		// ------------------------------------------------------------
		// 4. Scan source
		//    - Enumerate directories and files
		//    - Apply include / exclude rules
		//    - Count files and total bytes
		//    - Update progress (phase = Scanning)
		// ------------------------------------------------------------

		BackupScanRequest scanRequest = new()
		{
			SourcePath = plan.SourcePath,
			Recursive = plan.Recursive,
			IncludePatterns = plan.IncludePatterns,
			ExcludePatterns = plan.ExcludePatterns,
		};

		Progress<BackupScanProgress> scanProgress = new(sp =>
		{
			_currentProgress = _currentProgress with
			{
				CurrentPhase = BackupProgressPhase.Scanning,
				DirectoriesTraversed = sp.DirectoriesTraversed,
				FilesDiscovered = sp.FilesDiscovered,
			};
			progress?.Report(_currentProgress);
		});

		await foreach(BackupItem item in _scanner.ScanAsync(traversal, scanRequest, scanProgress, cancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			BackupRecord record = new() { Item = item };

			repository.Add(record);
		}

		// ------------------------------------------------------------
		// 4b. Resume — match scanned items against persisted summary
		//      to restore DestinationPath and processing Status
		// ------------------------------------------------------------

		BackupSummary? existingSummary = await _summaryStore.LoadAsync(cancellationToken);

		if(existingSummary is not null)
		{
			Dictionary<string, BackupSummaryItem> summaryItemsById = existingSummary.Items
				.ToDictionary(i => i.Id);

			HashSet<string> matchedIds = new();
			foreach(BackupRecord record in repository.GetAll())
			{
				if(summaryItemsById.TryGetValue(record.Item.Id, out BackupSummaryItem? match))
				{
					matchedIds.Add(record.Item.Id);
					record.DestinationPath = match.DestinationPath;
					record.Status = match.IsCompleted
						? BackupItemStatus.Succeeded
						: BackupItemStatus.Pending;
				}
			}

			int added = repository.GetAll().Count(r => !matchedIds.Contains(r.Item.Id));
			int removed = existingSummary.Items.Count(i => !summaryItemsById.ContainsKey(i.Id));
			bool hasChanges = added > 0 || removed > 0;

			if(hasChanges)
			{
				switch(plan.ResumeBehavior)
				{
					case SessionResumeStrategy.Abort:
						throw new InvalidOperationException(
							$"Source has changed: {added} file(s) added, {removed} file(s) removed.");

					case SessionResumeStrategy.Continue:
						break;

					case SessionResumeStrategy.Restart:
						await _summaryStore.DeleteAsync(cancellationToken);
						break;
				}
			}
		}

		BackupRunnerFactoryCreateRequest runnerFactoryCreateRequest = new()
		{
			MaxDegreeOfParallelism = plan.MaxDegreeOfParallelism
		};
		IBackupRunner runner = _backupRunnerFactory.Create(runnerFactoryCreateRequest);

		BackupRunnerRequest runnerRequest = new()
		{
		};

		Progress<BackupRunnerProgress> runnerProgress = new(rp =>
		{
			_currentProgress = _currentProgress with
			{
				CurrentPhase = rp.CurrentPhase,
				TotalFilesSelected = rp.TotalFilesSelected,
				FilesSucceeded = rp.FilesSucceeded,
				FilesSkipped = rp.FilesSkipped,
				FilesFailed = rp.FilesFailed,
				ActiveFiles = rp.ActiveFiles,
			};
			progress?.Report(_currentProgress);
		});

		await runner.RunAsync(runnerRequest, sessionKey, runnerProgress, cancellationToken);


		// ------------------------------------------------------------
		// 5. Prepare destination
		//    - Ensure destination is accessible
		//    - Create required directories
		// ------------------------------------------------------------

		// ------------------------------------------------------------
		// 6. Transfer files
		//    - Copy data from source to destination
		//    - Generate sidecar files
		//    - Update progress (phase = Transferring)
		// ------------------------------------------------------------

		// ------------------------------------------------------------
		// 7. Execute optional features (if enabled in BackupPlan)
		//    - Hashing
		//    - Metadata extraction
		//    - Verification
		//    - Timestamp correction
		// ------------------------------------------------------------

		// ------------------------------------------------------------
		// 8. Handle cancellation
		//    - Observe cancellationToken
		//    - Stop processing gracefully if requested
		// ------------------------------------------------------------

		// ------------------------------------------------------------
		// 9. Finalize backup result
		//    - Collect final counters
		//    - Determine final phase (Completed / Failed / Cancelled)
		//    - Set failure reason if applicable
		// ------------------------------------------------------------

		// ------------------------------------------------------------
		// 10. Return BackupResult
		// ------------------------------------------------------------

		throw new NotImplementedException("BackupEngine is not yet implemented.");
	}
}
