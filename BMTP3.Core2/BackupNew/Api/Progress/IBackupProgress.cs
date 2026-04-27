using System;
using System.Collections.Generic;
using BMTP3.Core2.BackupNew.Api.Progress.Enums;

namespace BMTP3.Core2.BackupNew.Api.Progress;

/// <summary>
///     Represents a point-in-time snapshot of a running backup job.
///     This is a live dashboard — not the final report.
/// </summary>
public interface IBackupProgress
{
	/// <summary>
	///     The current lifecycle state of the job.
	/// </summary>
	BackupState State { get; }
	/// <summary>
	///     The current work phase. Only meaningful when State is Running.
	/// </summary>
	BackupPhase Phase { get; }

	/// <summary>
	///     When the job started.
	/// </summary>
	DateTimeOffset StartedAt { get; }
	/// <summary>
	///     Elapsed time since the job started.
	/// </summary>
	TimeSpan Elapsed { get; }

	/// <summary>
	///     Number of directories traversed so far.
	/// </summary>
	int DirectoriesTraversed { get; }
	/// <summary>
	///     The directory currently being scanned. Empty when not scanning.
	/// </summary>
	string CurrentDirectory { get; }
	/// <summary>
	///     Total number of files discovered by the scanner.
	/// </summary>
	int FilesDiscovered { get; }
	/// <summary>
	///     Number of files excluded by filter rules during scanning.
	/// </summary>
	int FilesExcluded { get; }
	/// <summary>
	///     Number of directories or files that could not be read during scanning.
	/// </summary>
	int ScanErrors { get; }

	/// <summary>
	///     Total bytes to process (known so far — grows during Traversing phase).
	/// </summary>
	long BytesTotal { get; }
	/// <summary>
	///     Total bytes processed so far.
	/// </summary>
	long BytesProcessed { get; }

	/// <summary>
	///     Number of files fully processed (succeeded + skipped + failed).
	/// </summary>
	int FilesProcessed { get; }
	/// <summary>
	///     Number of files successfully copied.
	/// </summary>
	int FilesSucceeded { get; }
	/// <summary>
	///     Number of files skipped.
	/// </summary>
	int FilesSkipped { get; }
	/// <summary>
	///     Number of files that failed.
	/// </summary>
	int FilesFailed { get; }

	/// <summary>
	///     Current transfer speed in bytes per second.
	/// </summary>
	long BytesPerSecond { get; }
	/// <summary>
	///     Overall completion percentage (0–100).
	/// </summary>
	double PercentageComplete { get; }

	/// <summary>
	///     Files currently being processed by the pipeline.
	/// </summary>
	IReadOnlyList<FileProgress> ActiveFiles { get; }

	/// <summary>
	///     A rolling window of recent events for display in the UI.
	///     Oldest events are dropped as new ones arrive.
	/// </summary>
	IReadOnlyList<BackupEvent> RecentEvents { get; }
}
