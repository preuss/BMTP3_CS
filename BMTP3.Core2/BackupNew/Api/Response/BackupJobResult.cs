using System;
using System.Collections.Generic;
using BMTP3.Core2.BackupNew.Api.Progress;
using BMTP3.Core2.BackupNew.Api.Progress.Enums;
using BMTP3.Core2.BackupNew.Domain.Errors;

namespace BMTP3.Core2.BackupNew.Api.Response;

/// <summary>
///     The final report of a completed backup job.
///     Returned by BackupEngine.RunAsync().
/// </summary>
public class BackupJobResult
{
	/// <summary>
	///     The name of the job that was executed.
	/// </summary>
	public string JobName { get; init; } = string.Empty;

	/// <summary>
	///     When the job started.
	/// </summary>
	public DateTimeOffset StartTime { get; init; }
	/// <summary>
	///     When the job ended.
	/// </summary>
	public DateTimeOffset EndTime { get; init; }
	/// <summary>
	///     Total duration of the job.
	/// </summary>
	public TimeSpan Duration => EndTime - StartTime;

	/// <summary>
	///     How the job ended.
	/// </summary>
	public BackupState State { get; init; }
	/// <summary>
	///     Why the job stopped. None if Completed.
	/// </summary>
	public StopReason StopReason { get; init; }
	/// <summary>
	///     The final progress snapshot at the moment the job ended.
	///     Contains all counters, active files, and recent events.
	/// </summary>
	public BackupProgress FinalProgress { get; init; } = new();

	/// <summary>
	///     Critical errors that affected the entire job
	///     (e.g., destination full, pipeline crash).
	/// </summary>
	public IReadOnlyList<string> Errors { get; init; } = new List<string>();
	/// <summary>
	///     Details of individual files that failed.
	/// </summary>
	public IReadOnlyList<ErrorLog> FailedItems { get; init; } = new List<ErrorLog>();

	/// <summary>
	///     Total number of files discovered by the scanner.
	/// </summary>
	public int FilesDiscovered => FinalProgress.FilesDiscovered;
	/// <summary>
	///     Number of files successfully copied.
	/// </summary>
	public int FilesSucceeded => FinalProgress.FilesSucceeded;
	/// <summary>
	///     Number of files skipped.
	/// </summary>
	public int FilesSkipped => FinalProgress.FilesSkipped;
	/// <summary>
	///     Number of files that failed.
	/// </summary>
	public int FilesFailed => FinalProgress.FilesFailed;
	/// <summary>
	///     Total bytes processed.
	/// </summary>
	public long BytesProcessed => FinalProgress.BytesProcessed;
}
