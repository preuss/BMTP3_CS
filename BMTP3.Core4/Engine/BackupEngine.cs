using BMTP3.Core4.Api;
using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Engine.Exceptions;
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
		// 5. Prepare destination
		//    - Create root destination folder
		//    - Create .bmtp3 subfolder for session data (session.json, logs)
		//    - Fail if destination is not accessible
		// ------------------------------------------------------------
		Directory.CreateDirectory(plan.Destination);
		string metadataPath = Path.Combine(plan.Destination, ".bmtp3");
		Directory.CreateDirectory(metadataPath);

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

		BackupSummary? resumeSummary = await _summaryStore.LoadAsync(cancellationToken);

		if(resumeSummary != null)
		{
			IReadOnlyList<BackupRecord> records = repository.GetAll();

			Dictionary<string, BackupSummaryItem> summaryItemsById = resumeSummary.Items
				.ToDictionary(i => i.Id);

			HashSet<string> recordIds = records
				.Select(r => r.Item.Id)
				.ToHashSet();

			int added = records.Count(r => !summaryItemsById.ContainsKey(r.Item.Id));
			int removed = resumeSummary.Items.Count(i => !recordIds.Contains(i.Id));
			bool hasChanges = added > 0 || removed > 0;

			bool applyResumeState = true;

			if(hasChanges)
			{
				switch(plan.ResumeBehavior)
				{
					case SessionResumeStrategy.Abort:
						throw new SessionResumeMismatchException(added, removed);

					case SessionResumeStrategy.Continue:
						applyResumeState = true;
						break;

					case SessionResumeStrategy.Restart:
						await _summaryStore.DeleteAsync(cancellationToken);
						applyResumeState = false;
						break;
					default:
						throw new InvalidOperationException($"Unsupported resume behavior: {plan.ResumeBehavior}");
				}
			}

			if(applyResumeState)
			{
				foreach(BackupRecord record in records)
				{
					if(summaryItemsById.TryGetValue(record.Item.Id, out BackupSummaryItem? match))
					{
						record.DestinationPath = match.DestinationPath;
						record.Status = match.IsCompleted
							? BackupItemStatus.Succeeded
							: BackupItemStatus.Pending;
					}
				}
			}
		}
		// Save session, to update it.
		resumeSummary = new BackupSummary
		{
			SessionId = sessionKey.SessionId,
			SourceRoot = sessionKey.SourceIdentity,
			CreatedAt = DateTimeOffset.UtcNow,
			Items = repository.GetAll()
				.Select(record =>
				{
					long length = record.Item.Content.Length > (ulong)long.MaxValue
						? long.MaxValue
						: (long)record.Item.Content.Length;

					return new BackupSummaryItem
					{
						Id = record.Item.Id,
						SourcePath = record.Item.SourcePath,
						RelativePath = record.Item.RelativePath,
						FileName = record.Item.FileName,
						Length = length,
						LastModified = record.Item.DateModified,
						DateCreated = record.Item.DateCreated,
						DateAuthored = record.Item.DateAuthored,
						DestinationPath = record.DestinationPath,
						IsCompleted = record.Status == BackupItemStatus.Succeeded,
						CompletedAt = record.Status == BackupItemStatus.Succeeded ? DateTimeOffset.UtcNow : null,
						ErrorMessage = record.Status == BackupItemStatus.Failed ? "Processing failed." : null,
					};
				})
				.ToList(),
		};

		await _summaryStore.SaveAsync(resumeSummary, cancellationToken);

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

	private static BackupSummaryItem ToSummaryItem(BackupRecord record)
	{
		long length = record.Item.Content.Length > (ulong)long.MaxValue
			? long.MaxValue
			: (long)record.Item.Content.Length;

		return new BackupSummaryItem
		{
			Id = record.Item.Id,
			SourcePath = record.Item.SourcePath,
			RelativePath = record.Item.RelativePath,
			FileName = record.Item.FileName,
			Length = length,
			LastModified = record.Item.DateModified,
			DateCreated = record.Item.DateCreated,
			DateAuthored = record.Item.DateAuthored,
			DestinationPath = record.DestinationPath,
			IsCompleted = record.Status == BackupItemStatus.Succeeded,
			CompletedAt = record.Status == BackupItemStatus.Succeeded ? DateTimeOffset.UtcNow : null,
			ErrorMessage = record.Status == BackupItemStatus.Failed ? "Processing failed." : null,
		};
	}
}
