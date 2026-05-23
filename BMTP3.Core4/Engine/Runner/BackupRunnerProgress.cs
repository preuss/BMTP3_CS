using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Api.Models.Enums;
using System.Collections.Immutable;

namespace BMTP3.Core4.Engine.Runner;

/// <summary>
/// Snapshot of runner progress.
/// </summary>
internal sealed record BackupRunnerProgress
{
	// ---------------------------------------------------------------------
	// Phase
	// ---------------------------------------------------------------------

	/// <summary>
	/// The current high-level phase of the backup job.
	/// </summary>
	public BackupProgressPhase CurrentPhase { get; init; }
	

	// ---------------------------------------------------------------------
	// Processing
	// ---------------------------------------------------------------------

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
	// Active files
	// ---------------------------------------------------------------------

	/// <summary>
	/// Snapshot of files currently active in the backup workflow.
	/// Presence does not imply parallel execution.
	/// </summary>
	public IReadOnlyList<BackupProgressItem> ActiveFiles { get; init; } = ImmutableList<BackupProgressItem>.Empty;
}
