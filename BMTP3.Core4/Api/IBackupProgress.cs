using System;
using System.Collections.Generic;
using BMTP3.Core4.Models.Enums;

namespace BMTP3.Core4.Api;

/// <summary>
/// Represents a snapshot of the current progress state of an ongoing backup job.
/// The interface exposes only factual, observable data known by the backup engine.
/// </summary>
public record BackupProgress
{
	// ---------------------------------------------------------------------
	// Phase
	// ---------------------------------------------------------------------

	/// <summary>
	/// The current high-level phase of the backup job.
	/// </summary>
	public BackupPhase CurrentPhase { get; init; }


	// ---------------------------------------------------------------------
	// Discovery
	// ---------------------------------------------------------------------

	/// <summary>
	/// Total number of directories scanned so far.
	/// </summary>
	public int DirectoriesScanned { get; init; }

	/// <summary>
	/// Total number of files discovered so far.
	/// </summary>
	public int FilesDiscovered { get; init; }

	/// <summary>
	/// Total size in bytes of all discovered files.
	/// </summary>
	public long BytesTotal { get; init; }


	// ---------------------------------------------------------------------
	// Processing
	// ---------------------------------------------------------------------

	/// <summary>
	/// Total number of files processed so far.
	/// This includes succeeded, skipped, and failed files.
	/// </summary>
	public int FilesProcessed { get; init; }

	/// <summary>
	/// Number of files successfully backed up.
	/// </summary>
	public int FilesSucceeded { get; init; }

	/// <summary>
	/// Number of files skipped intentionally.
	/// </summary>
	public int FilesSkipped { get; init; }

	/// <summary>
	/// Number of files that failed processing.
	/// </summary>
	public int FilesFailed { get; init; }


	// ---------------------------------------------------------------------
	// Bytes
	// ---------------------------------------------------------------------

	/// <summary>
	/// Total number of bytes successfully processed so far.
	/// </summary>
	public long BytesProcessed { get; init; }


	// ---------------------------------------------------------------------
	// Active files
	// ---------------------------------------------------------------------

	/// <summary>
	/// Snapshot of files currently active in the backup workflow.
	/// Presence does not imply parallel execution.
	/// </summary>
	public IReadOnlyList<IBackupProgressItem> ActiveFiles { get; init; }
}