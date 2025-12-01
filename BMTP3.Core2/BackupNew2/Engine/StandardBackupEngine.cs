using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BMTP3.Core2.BackupNew2.Interfaces;
using BMTP3.Core2.BackupNew2.Models;
using BMTP3.Core2.BackupNew2.Models.Configuration;
using BMTP3.Core2.BackupNew2.Traversal; // Added for scanner types (Important new line)

namespace BMTP3.Core2.BackupNew2.Engine;

public class StandardBackupEngine : IBackupEngine
{
	private readonly IEnumerable<IBackupScanner> _allScanners; // Changed from single _scanner to all scanners
	private readonly IEnumerable<IBackupStep> _pipelineSteps;
	private readonly IBackupStateRepository _stateRepository;

	public StandardBackupEngine(
		IEnumerable<IBackupScanner> allScanners, // Now receives all scanners
		IEnumerable<IBackupStep> pipelineSteps,
		IBackupStateRepository stateRepository)
	{
		_allScanners = allScanners;
		_pipelineSteps = pipelineSteps;
		_stateRepository = stateRepository;
	}

	public async Task<BackupJobResult> ExecuteJobAsync(BackupJob job, IProgress<BackupProgress> progress, CancellationToken ct)
	{
		var result = new BackupJobResult
		{
			StartTime = DateTime.UtcNow,
			JobName = job.Name,
			Status = JobStatus.Running
		};

		var currentProgress = new BackupProgress();
		var stopWatch = Stopwatch.StartNew();

		try
		{
			// 1. Select the appropriate scanner based on job.SourceType
			IBackupScanner selectedScanner;
			if(job.SourceType == SourceType.MediaDevice)
			{
				selectedScanner = _allScanners.OfType<MediaDeviceScanner>().FirstOrDefault()
									 ?? throw new InvalidOperationException("MediaDeviceScanner not found for MTP source type.");
			} else if(job.SourceType == SourceType.FileSystem)
			{
				selectedScanner = _allScanners.OfType<FileSystemScanner>().FirstOrDefault()
									 ?? throw new InvalidOperationException("FileSystemScanner not found for FileSystem source type.");
			} else
			{
				throw new NotSupportedException($"SourceType '{job.SourceType}' is not supported.");
			}

			// 2. Load State
			await _stateRepository.LoadAsync();

			// 3. Scan & Process Loop
			await foreach(var item in selectedScanner.ScanAsync(job, ct)) // Use selected scanner
			{
				ct.ThrowIfCancellationRequested();

				currentProgress.TotalItemsDiscovered++;
				progress?.Report(currentProgress);

				try
				{
					// Run Pipeline
					foreach(var step in _pipelineSteps)
					{
						// Responsiveness check inside the pipeline loop
						ct.ThrowIfCancellationRequested();

						try
						{
							await step.ExecuteAsync(item, job, ct);
						} catch(Exception stepEx) when(stepEx is not OperationCanceledException)
						{
							// Step failure logic
							item.Fail(stepEx.Message, step.Name, stepEx);
							// If a step fails, we generally break the pipeline for this item, 
							// unless we implement specific recovery logic.
							break;
						}
					}

					// Record Stats
					if(item.ErrorInfo != null)
					{
						currentProgress.ItemsFailed++;
						result.FailedItems.Add(item.ErrorInfo); // Assuming Result has a list of errors
					} else
					{
						if(item.Action == BackupActionType.Copy || item.Action == BackupActionType.Rename)
						{
							currentProgress.ItemsProcessed++;
							// Use TryGet here in case metadata is missing, or rely on the step to ensure its present
							long bytes = item.Metadata.Get<long?>(MetadataKey.SizeBytes) ?? 0;
							currentProgress.BytesProcessed += bytes;
						} else if(item.Action == BackupActionType.Skip)
						{
							currentProgress.ItemsSkipped++;
						}
					}
				} catch(OperationCanceledException)
				{
					throw; // Re-throw to outer handler
				} catch(Exception itemEx)
				{
					// Catastrophic item failure (should be caught inside steps, but safety net)!
					currentProgress.ItemsFailed++;
					// This error is less specific than item.ErrorInfo, consider adding it to result.Errors if needed.
				}

				progress?.Report(currentProgress);
			}

			// 4. Save State
			await _stateRepository.SaveAsync();

			result.Status = JobStatus.Completed;
		} catch(OperationCanceledException)
		{
			result.Status = JobStatus.Cancelled;
			// Save state even on cancel to preserve work done so far
			await _stateRepository.SaveAsync();
		} catch(Exception ex)
		{
			result.Status = JobStatus.Failed;
			result.GlobalError = ex.Message;
		} finally
		{
			stopWatch.Stop();
			result.EndTime = DateTime.UtcNow;
			//result.Duration = stopWatch.Elapsed;
			result.FinalProgress = currentProgress;
		}

		return result;
	}
}