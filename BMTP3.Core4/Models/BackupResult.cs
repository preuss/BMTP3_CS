using BMTP3.Core4.Models.Enums;

namespace BMTP3.Core4.Models;

/// <summary>
/// Represents the final result of a completed backup job.
/// This is an immutable summary describing the outcome and statistics
/// of a single backup execution.
/// </summary>
public sealed record BackupResult
{
	// ---------------------------------------------------------------------
	// Identification
	// ---------------------------------------------------------------------

	/// <summary>
	/// The name of the backup job.
	/// </summary>
	public string Name { get; init; } = string.Empty;


	// ---------------------------------------------------------------------
	// Outcome
	// ---------------------------------------------------------------------

	/// <summary>
	/// The final phase reached by the backup job.
	/// </summary>
	public BackupPhase FinalPhase { get; init; }

	/// <summary>
	/// The reason why the backup job failed or stopped.
	/// Null if the backup completed successfully or was cancelled intentionally.
	/// </summary>
	public BackupErrorCode? FailureReason { get; init; }


	// ---------------------------------------------------------------------
	// Discovery summary
	// ---------------------------------------------------------------------

	/// <summary>
	/// The total number of directories scanned.
	/// </summary>
	public int DirectoriesScanned { get; init; }

	/// <summary>
	/// The total number of files discovered.
	/// </summary>
	public int FilesDiscovered { get; init; }

	/// <summary>
	/// The total size in bytes of all discovered files.
	/// </summary>
	public long BytesTotal { get; init; }


	// ---------------------------------------------------------------------
	// Processing summary
	// ---------------------------------------------------------------------

	/// <summary>
	/// The total number of files processed.
	/// </summary>
	public int FilesProcessed { get; init; }

	/// <summary>
	/// The number of files successfully backed up.
	/// </summary>
	public int FilesSucceeded { get; init; }

	/// <summary>
	/// The number of files skipped.
	/// </summary>
	public int FilesSkipped { get; init; }

	/// <summary>
	/// The number of files that failed processing.
	/// </summary>
	public int FilesFailed { get; init; }


	// ---------------------------------------------------------------------
	// Bytes
	// ---------------------------------------------------------------------

	/// <summary>
	/// The total number of bytes successfully processed.
	/// </summary>
	public long BytesProcessed { get; init; }
}
