using System.Collections.Concurrent;
using BMTP3.Core2.BackupNew.Api;
using BMTP3.Core2.BackupNew.Api.Progress.Enums;
using BMTP3.Core2.BackupNew.Api.Progress;
using BMTP3.Core2.BackupNew.Domain.Item;
// For ItemResultState

namespace BMTP3.Core2.BackupNew.Engine.Internal;

/// <summary>
///     Internal thread-safe tracker for backup progress.
///     Maintains state and produces snapshots for the API.
/// </summary>
public class ProgressTracker
{
	// --- Active Files (Thread-Safe Dictionary) ---
	// Key: ItemId (unique GUID string per running file)
	private readonly ConcurrentDictionary<string, FileProgress> _activeFiles = new();
	private long _bytesProcessed;

	private long _bytesTotal;

	// --- Global Phase ---
private volatile BackupPhase _currentPhase = BackupPhase.Initializing;

	// --- Discovery Counters (Interlocked) ---
	private int _directoriesTraversed;
	private int _filesFailed;
	private int _filesSkipped;

	// --- Processing Counters (Interlocked) ---
	private int _filesSucceeded;
	private int _filesTotal;

	// --- Phase Management ---

	public void SetPhase(BackupPhase phase)
	{
		_currentPhase = phase;
	}

	// --- Discovery Reporting ---

	public void AddDiscovery(bool isDirectory, long size = 0)
	{
		if (isDirectory)
		{
			Interlocked.Increment(ref _directoriesTraversed);
		}
		else
		{
			Interlocked.Increment(ref _filesTotal);
			Interlocked.Add(ref _bytesTotal, size);
		}
	}

	// --- Active Item Management ---

	/// <summary>
	///     Starts tracking a file or updates its phase.
	/// </summary>
	public void UpdateItemPhase(string itemId, string sourcePath, string fileName, string relativePath, FilePhase phase,
		ulong totalBytes)
	{
       _activeFiles.AddOrUpdate(itemId,
		   // Add new
		   key => new FileProgress
		   {
			   SourcePath = sourcePath,
			   FileName = fileName,
			   RelativePath = relativePath,
			   Phase = phase,
			   BytesTotal = (long)totalBytes,
			   BytesProcessed = 0
		   },
		   // Update existing
		   (key, existing) =>
		   {
			   // Create a new FileProgress with updated values (immutability for init-only)
			   return new FileProgress
			   {
				   SourcePath = existing.SourcePath,
				   FileName = existing.FileName,
				   RelativePath = existing.RelativePath,
				   Phase = phase,
				   BytesTotal = existing.BytesTotal == 0 ? (long)totalBytes : existing.BytesTotal,
				   BytesProcessed = existing.BytesProcessed,
				   RetryAttempt = existing.RetryAttempt,
				   StartedAt = existing.StartedAt
			   };
		   });
	}

	/// <summary>
	///     Updates the byte progress of an active file.
	/// </summary>
	public void UpdateItemBytes(string itemId, ulong bytesProcessed)
	{
       if (_activeFiles.TryGetValue(itemId, out FileProgress? progress))
	   {
		   // Replace with a new FileProgress (immutability for init-only)
		   FileProgress updated = new FileProgress
		   {
			   SourcePath = progress.SourcePath,
			   FileName = progress.FileName,
			   RelativePath = progress.RelativePath,
			   Phase = progress.Phase,
			   BytesTotal = progress.BytesTotal,
			   BytesProcessed = (long)bytesProcessed,
			   RetryAttempt = progress.RetryAttempt,
			   StartedAt = progress.StartedAt
		   };
		   _activeFiles[itemId] = updated;
	   }
	}

	/// <summary>
	///     Completes an item: Removes from active list and updates global stats.
	/// </summary>
	public void CompleteItem(string itemId, ItemResultState result, long totalBytes)
	{
		// 1. Remove from Active
		_activeFiles.TryRemove(itemId, out _);

		// 2. Update Globals
		switch (result)
		{
			case ItemResultState.Success:
				Interlocked.Increment(ref _filesSucceeded);
				Interlocked.Add(ref _bytesProcessed, totalBytes);
				break;
			case ItemResultState.Skipped:
				Interlocked.Increment(ref _filesSkipped);
				// Usually we count skipped bytes as processed so the bar reaches 100%
				Interlocked.Add(ref _bytesProcessed, totalBytes);
				break;
			case ItemResultState.Failed:
				Interlocked.Increment(ref _filesFailed);
				// Failed items typically don't contribute to "bytes processed" in terms of data moved,
				// but for a progress bar it might be useful to count them as 'done'.
				Interlocked.Add(ref _bytesProcessed, totalBytes);
				break;
		}
	}

	// --- Snapshot Generation ---

	public BackupProgress GetSnapshot()
	{
		// Calculate total processed for convenience
		int processed = _filesSucceeded + _filesFailed + _filesSkipped;

		// Snapshot active files (ToArray is thread-safe on ConcurrentDictionary values)
		List<FileProgress> activeSnapshot = _activeFiles.Values.Select(fp => new FileProgress
		{
			FileName = fp.FileName,
			RelativePath = fp.RelativePath,
			SourcePath = fp.SourcePath,
			Phase = fp.Phase,
			BytesTotal = fp.BytesTotal,
			BytesProcessed = fp.BytesProcessed
		}).ToList();

		return new BackupProgress
		{
			Phase = _currentPhase,

			DirectoriesTraversed = _directoriesTraversed,
			FilesDiscovered = _filesTotal,
			BytesTotal = _bytesTotal,

			FilesProcessed = processed,
			FilesSucceeded = _filesSucceeded,
			FilesSkipped = _filesSkipped,
			FilesFailed = _filesFailed,

			BytesProcessed = _bytesProcessed,

			ActiveFiles = activeSnapshot
		};
	}
}