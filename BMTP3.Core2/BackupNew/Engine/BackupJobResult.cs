using BMTP3.Core2.BackupNew.Errors;
using BMTP3.Core2.BackupNew.Models;

namespace BMTP3.Core2.BackupNew.Engine;

/// <summary>
/// Represents the summary and outcome of a completed backup job.
/// </summary>
public class BackupJobResult
{
	/// <summary>
	/// The name of the job that was executed.
	/// </summary>
	public string JobName { get; set; } = string.Empty;

	/// <summary>
	/// The time when the job execution started.
	/// </summary>
	public DateTime StartTime { get; set; }

	/// <summary>
	/// The time when the job execution ended.
	/// </summary>
	public DateTime EndTime { get; set; }

	/// <summary>
	/// The total duration of the job execution.
	/// </summary>
	public TimeSpan Duration => EndTime - StartTime;

	// --- Statistics ---

	/// <summary>
	/// Total number of files identified in the source before filtering.
	/// </summary>
	public int TotalFilesScanned { get; set; }

	/// <summary>
	/// Total number of files considered for processing after filters were applied.
	/// </summary>
	public int TotalFilesConsidered { get; set; }

	/// <summary>
	/// Number of files successfully copied to the destination.
	/// </summary>
	public int FilesCopied { get; set; }

	/// <summary>
	/// Number of files skipped due to configuration (e.g., already existing, excluded).
	/// </summary>
	public int FilesSkipped { get; set; }

	/// <summary>
	/// Number of files that failed to be processed (e.g., read errors, write errors).
	/// </summary>
	public int FilesFailed { get; set; }

	/// <summary>
	/// Total size in bytes of files successfully copied.
	/// </summary>
	public long TotalBytesCopied { get; set; }

	// --- Status & Errors ---

	/// <summary>
	/// The overall status of the job execution (Success, SuccessWithWarnings, Failed).
	/// </summary>
	public JobStatus Status { get; set; }

	/// <summary>
	/// List of critical errors that affected the entire job (e.g., source disconnected, destination full).
	/// </summary>
	public List<string> GlobalErrors { get; set; } = new();

	/// <summary>
	/// List of file-specific errors encountered during the job.
	/// </summary>
	public List<string> FileErrors { get; set; } = new(); // Adding a specific list for file errors for granularity
	public List<ErrorInfo?> FailedItems { get; internal set; } = new();
	public string GlobalError { get; internal set; }
	public BackupProgress FinalProgress { get; internal set; }
}
