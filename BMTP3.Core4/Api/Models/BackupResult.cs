using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Models.Enums;
using System.Collections.Immutable;

namespace BMTP3.Core4.Api.Models;

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
	public BackupResultState State { get; init; }
	public BackupResultFailureReason? FailureReason { get; init; }

	// ---------------------------------------------------------------------
	// Discovery summary
	// ---------------------------------------------------------------------

	// ---------------------------------------------------------------------
	// Processing summary
	// ---------------------------------------------------------------------

	public IReadOnlyList<BackupResultItem> ItemResults { get; init; } = ImmutableList<BackupResultItem>.Empty;
}
