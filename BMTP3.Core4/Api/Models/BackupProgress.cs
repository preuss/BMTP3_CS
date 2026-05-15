using System.Collections.Immutable;
using BMTP3.Core4.Api.Models.Enums;

namespace BMTP3.Core4.Api.Models;

/// <summary>
/// Represents a snapshot of the current progress state of an ongoing backup job.
/// The interface exposes only factual, observable data known by the backup engine.
/// </summary>
public record BackupProgress
{
	// ---------------------------------------------------------------------
	// Plan
	// ---------------------------------------------------------------------

	/// <summary>
	/// The source path from the backup plan that is being processed.
	/// </summary>
	public string SourcePath { get; init; } = string.Empty;

	/// <summary>
	/// The destination path from the backup plan that is being processed.
	/// </summary>
	public string DestinationPath { get; init; } = string.Empty;

	// ---------------------------------------------------------------------
	// Phase
	// ---------------------------------------------------------------------

	/// <summary>
	/// The current high-level phase of the backup job.
	/// </summary>
	public BackupProgressPhase CurrentPhase { get; init; }


	// ---------------------------------------------------------------------
	// Discovery
	// ---------------------------------------------------------------------

	/// <summary>
	/// Total number of directories traversed so far.
	/// </summary>
	public int DirectoriesTraversed { get; init; }

	/// <summary>
	/// Total number of files discovered during traversal so far.
	/// </summary>
	public int FilesDiscovered { get; init; }


	// ---------------------------------------------------------------------
	// Processing
	// ---------------------------------------------------------------------

	/// <summary>
	/// Total number of files selected for backup after filtering.
	/// </summary>
	public int TotalFilesSelected { get; init; }

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
	public IReadOnlyList<BackupProgressItem> ActiveFiles { get; init; } = ImmutableList<BackupProgressItem>.Empty;
}