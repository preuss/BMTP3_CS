using BMTP3.Core4.Models.Enums;

namespace BMTP3.Core4.Api;

/// <summary>
/// Represents a snapshot of the current progress state of an ongoing backup job.
/// The interface exposes only factual, observable data known by the backup engine.
/// </summary>
public interface IBackupProgress
{
	// ---------------------------------------------------------------------
	// Phase
	// ---------------------------------------------------------------------

	/// <summary>
	/// The current high-level phase of the backup job.
	/// </summary>
	BackupPhase CurrentPhase { get; }


	// ---------------------------------------------------------------------
	// Discovery
	// ---------------------------------------------------------------------

	/// <summary>
	/// Total number of directories scanned so far.
	/// </summary>
	int DirectoriesScanned { get; }

	/// <summary>
	/// Total number of files discovered so far.
	/// </summary>
	int FilesDiscovered { get; }

	/// <summary>
	/// Total size in bytes of all discovered files.
	/// </summary>
	long BytesTotal { get; }


	// ---------------------------------------------------------------------
	// Processing
	// ---------------------------------------------------------------------

	/// <summary>
	/// Total number of files processed so far.
	/// This includes succeeded, skipped, and failed files.
	/// </summary>
	int FilesProcessed { get; }

	/// <summary>
	/// Number of files successfully backed up.
	/// </summary>
	int FilesSucceeded { get; }

	/// <summary>
	/// Number of files skipped intentionally.
	/// </summary>
	int FilesSkipped { get; }

	/// <summary>
	/// Number of files that failed processing.
	/// </summary>
	int FilesFailed { get; }


	// ---------------------------------------------------------------------
	// Bytes
	// ---------------------------------------------------------------------

	/// <summary>
	/// Total number of bytes successfully processed so far.
	/// </summary>
	long BytesProcessed { get; }


	// ---------------------------------------------------------------------
	// Active files
	// ---------------------------------------------------------------------

	/// <summary>
	/// Snapshot of files currently active in the backup workflow.
	/// Presence does not imply parallel execution.
	/// </summary>
	IReadOnlyList<IFileProgress> ActiveFiles { get; }
}