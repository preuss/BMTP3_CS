using System;
using System.Collections.Generic;
using BMTP3.Core2.BackupNew.Api.Progress.Enums;

namespace BMTP3.Core2.BackupNew.Api.Progress;

/// <summary>
///     Immutable snapshot implementing IBackupProgress.
///     Created by ProgressTracker.GetSnapshot().
/// </summary>
public class BackupProgress : IBackupProgress
{
	/// <summary>
	///     The current lifecycle state of the job.
	/// </summary>
	public BackupState State { get; init; }
	/// <summary>
	///     The current work phase. Only meaningful when State is Running.
	/// </summary>
	public BackupPhase Phase { get; init; }

	/// <summary>
	///     When the job started.
	/// </summary>
	public DateTimeOffset StartedAt { get; init; }
	/// <summary>
	///     Elapsed time since the job started.
	/// </summary>
	public TimeSpan Elapsed { get; init; }

	/// <summary>
	///     Number of directories traversed so far.
	/// </summary>
	public int DirectoriesTraversed { get; init; }
	/// <summary>
	///     The directory currently being scanned. Empty when not scanning.
	/// </summary>
	public string CurrentDirectory { get; init; } = string.Empty;
	/// <summary>
	///     Total number of files discovered by the scanner.
	/// </summary>
	public int FilesDiscovered { get; init; }
	/// <summary>
	///     Number of files excluded by filter rules during scanning.
	/// </summary>
	public int FilesExcluded { get; init; }
	/// <summary>
	///     Number of directories or files that could not be read during scanning.
	/// </summary>
	public int ScanErrors { get; init; }

	/// <summary>
	///     Total bytes to process (known so far — grows during Traversing phase).
	/// </summary>
	public long BytesTotal { get; init; }
	/// <summary>
	///     Total bytes processed so far.
	/// </summary>
	public long BytesProcessed { get; init; }

	/// <summary>
	///     Number of files fully processed (succeeded + skipped + failed).
	/// </summary>
	public int FilesProcessed { get; init; }
	/// <summary>
	///     Number of files successfully copied.
	/// </summary>
	public int FilesSucceeded { get; init; }
	/// <summary>
	///     Number of files skipped.
	/// </summary>
	public int FilesSkipped { get; init; }
	/// <summary>
	///     Number of files that failed.
	/// </summary>
	public int FilesFailed { get; init; }

	/// <summary>
	///     Current transfer speed in bytes per second.
	/// </summary>
	public long BytesPerSecond { get; init; }
	/// <summary>
	///     Overall completion percentage (0–100).
	/// </summary>
	public double PercentageComplete { get; init; }

	/// <summary>
	///     Files currently being processed by the pipeline.
	/// </summary>
	public IReadOnlyList<FileProgress> ActiveFiles { get; init; } = new List<FileProgress>();
	/// <summary>
	///     A rolling window of recent events for display in the UI.
	///     Oldest events are dropped as new ones arrive.
	/// </summary>
	public IReadOnlyList<BackupEvent> RecentEvents { get; init; } = new List<BackupEvent>();
}
