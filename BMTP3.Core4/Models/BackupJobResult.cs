using System.Collections.Generic;
using BMTP3.Core4.Models.Enums;

namespace BMTP3.Core4.Models;

/// <summary>
/// Represents the final result of a completed backup job.
/// This is an immutable summary describing the outcome and statistics
/// of a single backup execution.
/// </summary>
public sealed record BackupJobResult
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

	// ---------------------------------------------------------------------
	// Discovery summary
	// ---------------------------------------------------------------------


	// ---------------------------------------------------------------------
	// Processing summary
	// ---------------------------------------------------------------------

	public IReadOnlyList<BackupJobResultItem> FileResults { get; init; } = [];
}
