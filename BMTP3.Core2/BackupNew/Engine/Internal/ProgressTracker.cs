using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using BMTP3.Core2.BackupNew.Api;
using BMTP3.Core2.BackupNew.Api.Enums;
using BMTP3.Core2.BackupNew.Domain.Item; // For ItemResultState

namespace BMTP3.Core2.BackupNew.Engine.Internal;

/// <summary>
/// Internal thread-safe tracker for backup progress.
/// Maintains state and produces snapshots for the API.
/// </summary>
public class ProgressTracker
{
    // --- Global Phase ---
    private volatile int _currentPhase = (int)BackupPhase.Starting;

    // --- Discovery Counters (Interlocked) ---
    private int _directoriesTraversed;
    private int _filesTotal;
    private long _bytesTotal;

    // --- Processing Counters (Interlocked) ---
    private int _filesSucceeded;
    private int _filesFailed;
    private int _filesSkipped;
    private long _bytesProcessed;

    // --- Active Files (Thread-Safe Dictionary) ---
    // Key: SourceFullPath (unique ID per running file)
    private readonly ConcurrentDictionary<string, FileProgress> _activeFiles = new();

    // --- Phase Management ---

    public void SetPhase(BackupPhase phase)
    {
        _currentPhase = (int)phase;
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
    /// Starts tracking a file or updates its phase.
    /// </summary>
    public void UpdateItemPhase(string sourcePath, string fileName, string relativePath, FilePhase phase, long totalBytes)
    {
        _activeFiles.AddOrUpdate(sourcePath,
            // Add new
            key => new FileProgress
            {
                SourcePath = sourcePath,
                FileName = fileName,
                RelativePath = relativePath,
                Phase = phase,
                BytesTotal = totalBytes,
                BytesProcessed = 0
            },
            // Update existing
            (key, existing) =>
            {
                existing.Phase = phase;
                // Ensure total bytes is set if discovered late
                if (existing.BytesTotal == 0) existing.BytesTotal = totalBytes;
                return existing;
            });
    }

    /// <summary>
    /// Updates the byte progress of an active file.
    /// </summary>
    public void UpdateItemBytes(string sourcePath, long bytesProcessed)
    {
        if (_activeFiles.TryGetValue(sourcePath, out var progress))
        {
            progress.BytesProcessed = bytesProcessed;
        }
    }

    /// <summary>
    /// Completes an item: Removes from active list and updates global stats.
    /// </summary>
    public void CompleteItem(string sourcePath, ItemResultState result, long totalBytes)
    {
        // 1. Remove from Active
        _activeFiles.TryRemove(sourcePath, out _);

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
        var activeSnapshot = _activeFiles.Values.Select(fp => new FileProgress 
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
            Phase = (BackupPhase)_currentPhase,
            
            DirectoriesTraversed = _directoriesTraversed,
            FilesTotal = _filesTotal,
            BytesTotal = _bytesTotal,

            FilesProcessed = processed,
            FilesSucceeded = _filesSucceeded,
            FilesFailed = _filesFailed,
            FilesSkipped = _filesSkipped,
            
            BytesProcessed = _bytesProcessed,

            ActiveFiles = activeSnapshot
        };
    }
}