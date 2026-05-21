using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Engine.Session;
using BMTP3.Core4.Models;
using BMTP3.Core4.Models.Enums;

namespace BMTP3.Core4.Engine.Runner;

internal sealed class SequentialBackupRunner : IBackupRunner
{
	public async Task<BackupResult> RunAsync(
		BackupRunnerRequest request,
		BackupSessionKey sessionKey,
		IProgress<BackupRunnerProgress>? progress,
		CancellationToken cancellationToken = default
	)
	{
		ArgumentNullException.ThrowIfNull(request);

		IReadOnlyList<BackupRecord> records = request.Records;
		
		BackupRunnerProgress currentProgress = new()
		{
			TotalFilesSelected = records.Count,
		};
		progress?.Report(currentProgress);

		// ------------------------------------------------------------
		// Phase 1 — Pre-processing: guard, count, separate Pending
		// ------------------------------------------------------------

		// Guard against invalid record statuses before starting the run.
		// This ensures that the progress reporting and result seeding logic can safely assume only valid statuses are present.
		ValidateStatusOfRecordItem(records);

		// Seed initial progress and result items based on existing record statuses.
		// Sends progress of count of total files, and seeds result items for any records that have already succeeded or been skipped in previous runs.
		IProgress<BackupRunnerProgress> seedProgress = new Progress<BackupRunnerProgress>(
			snapshot =>
			{
				currentProgress = snapshot;
				progress?.Report(snapshot);
			}
		);
		List<BackupResultItem> resultItems = SeedResultItems(records, currentProgress, seedProgress, cancellationToken);

		// Filter pending records for processing.
		List<BackupRecord> pendingRecords = FilterPendingRecords(records);

		// Update to Transferring phase after pre-processing is complete and pending records have been identified.
		currentProgress = currentProgress with
		{
			CurrentPhase = BackupProgressPhase.Transferring,
		};
		progress?.Report(currentProgress);

		// ------------------------------------------------------------
		// Phase 2 — Processing: file transfer for Pending records
		// ------------------------------------------------------------
		foreach(BackupRecord record in pendingRecords)
		{
			cancellationToken.ThrowIfCancellationRequested();

			BackupItem item = record.Item;
			string destinationPath = ResolveDestinationPath(request, item);
			record.DestinationPath = destinationPath;

			currentProgress = currentProgress with
			{
				ActiveFiles = new[]
				{
					new BackupProgressItem
					{
						RelativePath = item.RelativePath,
						Length = (long)item.Content.Length,
						Phase = BackupProgressItemPhase.Transferring,
					}
				}
			};
			progress?.Report(currentProgress);

			bool destinationFileCreated = false;

			try
			{
				// 1. Create parent directory
				string? parentDir = Path.GetDirectoryName(destinationPath);
				if(!string.IsNullOrEmpty(parentDir))
				{
					Directory.CreateDirectory(parentDir);
				}

				// 2. Check for collision
				if(File.Exists(destinationPath))
				{
					switch(request.CollisionStrategy)
					{
						case CollisionStrategy.Error:
							throw new IOException($"Destination already exists: {destinationPath}");

						// Tier 2+: Rename, Skip, Overwrite

						default:
							throw new InvalidOperationException($"Collision strategy '{request.CollisionStrategy}' is not supported in the current Tier.");
					}
				}

				// 3. Transfer file content
				await using(Stream sourceStream = await item.Content.OpenReadStreamAsync(cancellationToken))
				await using(FileStream destStream = File.Create(destinationPath))
				{
					destinationFileCreated = true;
					await sourceStream.CopyToAsync(destStream, cancellationToken);
				}

				// 4. Generate sidecar
				await WriteSidecarAsync(item, destinationPath, request.SidecarFormat, request.BackupStartTime, cancellationToken);

				record.Status = BackupItemStatus.Succeeded;
				record.StatusChangedAt = DateTimeOffset.UtcNow;
				currentProgress = currentProgress with
				{
					FilesSucceeded = currentProgress.FilesSucceeded + 1,
				};

				resultItems.Add(ToResultItem(item, destinationPath, BackupResultItemState.Succeeded));
			}
			catch(OperationCanceledException)
			{
				throw;
			}
			catch(NotImplementedException)
			{
				throw;
			}
			catch(Exception ex)
			{
				// Cleanup partial destination file to prevent deadlock on resume
				if(destinationFileCreated && File.Exists(destinationPath))
				{
					try { File.Delete(destinationPath); } catch { /* best-effort cleanup */ }
				}

				record.Status = BackupItemStatus.Failed;
				record.StatusChangedAt = DateTimeOffset.UtcNow;
				currentProgress = currentProgress with
				{
					FilesFailed = currentProgress.FilesFailed + 1,
				};

				resultItems.Add(ToResultItem(item, null, BackupResultItemState.Failed));

				if(request.StopOnError)
				{
					throw new InvalidOperationException($"Backup stopped because '{item.SourcePath}' failed.", ex);
				}
			}

			// Reset active files after each item to ensure progress is reported
			// even if the last few items fail or the file was pre-processed.
			currentProgress = currentProgress with
			{
				ActiveFiles = Array.Empty<BackupProgressItem>(),
			};
			progress?.Report(currentProgress);
		}

		BackupResultState resultState = currentProgress.FilesFailed > 0
			? BackupResultState.Failed
			: BackupResultState.Completed;

		return new BackupResult
		{
			// TODO: When Backup Name have been implemented, set this to a meaningful value instead of the SourceIdentity.
			Name = sessionKey.SourceIdentity,
			State = resultState,
			ItemResults = resultItems,
		};
	}

	private static void ValidateStatusOfRecordItem(IReadOnlyList<BackupRecord> records)
	{
		foreach(BackupRecord record in records)
		{
			switch(record.Status)
			{
				case BackupItemStatus.Pending:
				case BackupItemStatus.Succeeded:
				case BackupItemStatus.Skipped:
					//Legal status.
					break;

				case BackupItemStatus.Active:
					throw new InvalidOperationException($"Record '{record.Item.Id}' has status Active at run start. This may indicate a crash during a previous run.");

				case BackupItemStatus.Failed:
					throw new InvalidOperationException($"Record '{record.Item.Id}' has status Failed at run start. Failed items should have been converted to Pending during resume.");

				default:
					// Unknown status
					throw new InvalidOperationException($"Unexpected BackupItemStatus '{record.Status}' for record '{record.Item.Id}'.");
			}
		}
	}

	private static List<BackupResultItem> SeedResultItems(
		IReadOnlyList<BackupRecord> records,
		BackupRunnerProgress currentProgress,
		IProgress<BackupRunnerProgress>? progress, 
		CancellationToken cancellationToken = default
	)
	{
		List<BackupResultItem> resultItems = new(capacity: records.Count);

		foreach(BackupRecord record in records)
		{
			cancellationToken.ThrowIfCancellationRequested();

			switch(record.Status)
			{
				case BackupItemStatus.Succeeded:
					resultItems.Add(ToResultItem(record.Item, record.DestinationPath, BackupResultItemState.Succeeded));
					currentProgress = currentProgress with { FilesSucceeded = currentProgress.FilesSucceeded + 1 };
					progress?.Report(currentProgress);
					break;

				case BackupItemStatus.Skipped:
					resultItems.Add(ToResultItem(record.Item, record.DestinationPath, BackupResultItemState.Skipped));
					currentProgress = currentProgress with { FilesSkipped = currentProgress.FilesSkipped + 1 };
					progress?.Report(currentProgress);
					break;

				default:
					break;
			}
		}

		return resultItems;
	}

	private static List<BackupRecord> FilterPendingRecords(IReadOnlyList<BackupRecord> records)
	{
		List<BackupRecord> pendingRecords = new(capacity: records.Count);

		foreach(BackupRecord record in records)
		{
			if(record.Status == BackupItemStatus.Pending)
			{
				pendingRecords.Add(record);
			}
		}

		return pendingRecords;
	}

	private static string ResolveDestinationPath(BackupRunnerRequest request, BackupItem item)
	{
		return request.OutputStructureStrategy switch
		{
			OutputStructureStrategy.PreserveHierarchy => Path.Combine(request.Destination, item.RelativePath),

			OutputStructureStrategy.Flat => Path.Combine(request.Destination, item.FileName),

			OutputStructureStrategy.CustomPathPattern => throw new NotImplementedException("CustomPathPattern is not yet implemented."),

			_ => throw new InvalidOperationException($"Unexpected OutputStructureStrategy '{request.OutputStructureStrategy}'."),
		};
	}

	private static async Task WriteSidecarAsync(BackupItem item, string destinationPath, SidecarFormat sidecarFormat, DateTimeOffset backupStartTime, CancellationToken cancellationToken)
	{
		switch(sidecarFormat)
		{
			case SidecarFormat.Ini:
				await WriteIniSidecarAsync(item, destinationPath, backupStartTime, cancellationToken);
				break;

			case SidecarFormat.None:
			case SidecarFormat.Json:
				throw new NotImplementedException($"Sidecar format '{sidecarFormat}' is not yet implemented.");

			default:
				throw new InvalidOperationException($"Unexpected SidecarFormat '{sidecarFormat}'.");
		}
	}

	private static async Task WriteIniSidecarAsync(BackupItem item, string destinationPath, DateTimeOffset backupStartTime, CancellationToken cancellationToken)
	{
		string sidecarPath = destinationPath + ".ini";

		// TODO: Add [FileHash] section when hashing is implemented (Tier 3)
		// TODO: Add [DriveFileDetails] / [DriveDetails] section when Drive source is implemented
		string content =
			$"[Settings]{Environment.NewLine}" +
			$"OriginalFileName={item.FileName}{Environment.NewLine}" +
			$"CreateDateTime={item.DateCreated?.ToString("O")}{Environment.NewLine}" +
			$"LastAccessDateTime={item.DateAccessed?.ToString("O")}{Environment.NewLine}" +
			$"LastWriteDateTime={item.DateModified?.ToString("O")}{Environment.NewLine}" +
			$"MediaTakenDateTime={item.DateAuthored?.ToString("O")}{Environment.NewLine}" +
			$"RelativePath={item.RelativePath}{Environment.NewLine}" +
			$"{Environment.NewLine}" +
			$"[BackupInfo]{Environment.NewLine}" +
			$"BackupDateTime={backupStartTime:O}{Environment.NewLine}";

		await File.WriteAllTextAsync(sidecarPath, content, cancellationToken);
	}

	private static BackupResultItem ToResultItem(BackupItem item, string? destinationPath, BackupResultItemState state)
	{
		return new BackupResultItem
		{
			Id = item.Id,
			SourcePath = item.SourcePath,
			DestinationPath = destinationPath,
			Length = (long)item.Content.Length,
			State = state,
		};
	}
}
