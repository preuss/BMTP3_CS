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
			CurrentPhase = BackupProgressPhase.Transferring
		};
		progress?.Report(currentProgress);

		// ------------------------------------------------------------
		// Phase 1 — Pre-processing: guard, count, separate Pending
		// ------------------------------------------------------------

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

	// ------------------------------------------------------------
	// Phase 2 — Processing metoder (temp-file pipeline)
	// ------------------------------------------------------------

	private static async Task<IMoveableContent> DownloadToTempAsync(FileInfo tempFile, IContent sourceContent, IProgress<ulong>? fileProgress, CancellationToken cancellationToken)
	{
		await using Stream sourceStream = await sourceContent.OpenReadStreamAsync(cancellationToken);
		await using FileStream destStream = tempFile.Create();

		long bytesRead = 0;
		byte[] buffer = new byte[81920];
		int read;

		while((read = await sourceStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
		{
			await destStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
			bytesRead += read;
			fileProgress?.Report((ulong)bytesRead);
		}

		// TODO: Return new MoveableFileContent(tempFile.FullName) when implementation exists
		throw new NotImplementedException("IMoveableContent is not yet implemented.");
	}

	private static async Task<string> BuildAndSaveSidecarAsync(BackupItem item, string tempPath, SidecarFormat sidecarFormat, DateTimeOffset backupStartTime, CancellationToken cancellationToken)
	{
		string content = sidecarFormat switch
		{
			SidecarFormat.Ini => BuildIniSidecarContent(item, backupStartTime),

			SidecarFormat.None or SidecarFormat.Json => throw new NotImplementedException($"Sidecar format '{sidecarFormat}' is not yet implemented."),

			_ => throw new InvalidOperationException($"Unexpected SidecarFormat '{sidecarFormat}'."),
		};

		string sidecarPath = tempPath + ".ini";
		await File.WriteAllTextAsync(sidecarPath, content, cancellationToken);
		return sidecarPath;
	}

	private static string BuildIniSidecarContent(BackupItem item, DateTimeOffset backupStartTime)
	{
		return
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
	}

	private static string? ResolveTargetPath(string destinationPath, CollisionStrategy collisionStrategy)
	{
		if(!File.Exists(destinationPath))
		{
			return destinationPath;
		}

		switch(collisionStrategy)
		{
			case CollisionStrategy.Error:
				throw new IOException($"Destination already exists: {destinationPath}");

			// Tier 2+: Rename, Skip, Overwrite

			default:
				throw new InvalidOperationException($"Collision strategy '{collisionStrategy}' is not supported in the current Tier.");
		}
	}

	private static void CommitTransfer(string tempPath, string tempSidecarPath, string finalPath)
	{
		File.Move(tempPath, finalPath, overwrite: false);

		string finalSidecarPath = finalPath + ".ini";
		if(File.Exists(tempSidecarPath))
		{
			File.Move(tempSidecarPath, finalSidecarPath, overwrite: false);
		}
	}

	private static void CleanupTempFiles(string? tempPath, string? tempSidecarPath)
	{
		if(tempPath is not null && File.Exists(tempPath))
		{
			try { File.Delete(tempPath); } catch { /* best-effort cleanup */ }
		}

		if(tempSidecarPath is not null && File.Exists(tempSidecarPath))
		{
			try { File.Delete(tempSidecarPath); } catch { /* best-effort cleanup */ }
		}
	}

	// ------------------------------------------------------------
	// Hjælpemetoder — temp-sti og filnavn
	// ------------------------------------------------------------

	private static DirectoryInfo ResolveTempDirectoryPath(string destination, DateTimeOffset backupStartTime, BackupSessionKey sessionKey)
	{
		string timestamp = backupStartTime.ToString("yyyyMMdd_HHmmss");
		string dirName = $"{timestamp}_{sessionKey.SessionId}";
		return new DirectoryInfo(Path.Combine(destination, ".tmp", dirName));
	}

	private static void PrepareTempDirectory(DirectoryInfo tempDir)
	{
		tempDir.Create();
	}

	private static string ResolveTempFileName(BackupItem item, DirectoryInfo tempDir, string extension = ".tmp")
	{
		if(string.IsNullOrEmpty(item.Id))
		{
			return Guid.NewGuid().ToString("N") + extension;
		}

		string baseName = item.Id + extension;

		bool hasInvalidChars = baseName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0;
		bool fileExists = File.Exists(Path.Combine(tempDir.FullName, baseName));

		if(!hasInvalidChars && !fileExists)
		{
			return baseName;
		}

		return Guid.NewGuid().ToString("N") + extension;
	}
}
