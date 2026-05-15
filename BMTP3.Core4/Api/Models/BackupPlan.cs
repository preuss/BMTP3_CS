using BMTP3.Core4.Models.Enums;

namespace BMTP3.Core4.Api.Models;

/// <summary>
/// Represents the immutable configuration for a single backup job.
/// The backup plan describes WHAT should be backed up and HOW it should behave,
/// but contains no execution or implementation details.
/// </summary>
public sealed record BackupPlan
{
	// ---------------------------------------------------------------------
	// Identification
	// ---------------------------------------------------------------------

	/// <summary>
	/// A human-readable name for the backup job.
	/// </summary>
	public string Name { get; init; } = string.Empty;


	// ---------------------------------------------------------------------
	// Source
	// ---------------------------------------------------------------------

	/// <summary>
	/// The type of source to back up (e.g. filesystem or media device).
	/// </summary>
	public BackupSourceType SourceType { get; init; }

	/// <summary>
	/// The root path or identifier of the source.
	/// For filesystem sources, this is a directory path.
	/// For device sources, this may be a device id.
	/// </summary>
	public string Source { get; init; } = string.Empty;

	/// <summary>
	/// Indicates whether subdirectories should be included.
	/// </summary>
	public bool Recursive { get; init; } = true;

	/// <summary>
	/// Optional include patterns applied during scanning.
	/// </summary>
	public IReadOnlyList<string>? IncludePatterns { get; init; }

	/// <summary>
	/// Optional exclude patterns applied during scanning.
	/// </summary>
	public IReadOnlyList<string>? ExcludePatterns { get; init; }


	// ---------------------------------------------------------------------
	// Destination
	// ---------------------------------------------------------------------

	/// <summary>
	/// The output directory where the backup will be stored.
	/// </summary>
	public string Destination { get; init; } = string.Empty;

	/// <summary>
	/// Determines how the output directory structure is created.
	/// </summary>
	public OutputStructure OutputStructure { get; init; }

	/// <summary>
	/// Defines how name collisions at the destination are handled.
	/// </summary>
	public CollisionStrategy CollisionStrategy { get; init; }


	// ---------------------------------------------------------------------
	// Behavior
	// ---------------------------------------------------------------------

	/// <summary>
	/// If true, the backup performs no write operations.
	/// The process is simulated only.
	/// </summary>
	public bool DryRun { get; init; }

	/// <summary>
	/// If true, the backup stops immediately on the first fatal error.
	/// </summary>
	public bool StopOnError { get; init; }

	/// <summary>
	/// If true, existing destination files are skipped.
	/// </summary>
	public bool SkipExisting { get; init; }


	// ---------------------------------------------------------------------
	// Optional features
	// ---------------------------------------------------------------------

	/// <summary>
	/// Indicates whether file hashing is enabled.
	/// </summary>
	public bool EnableHashing { get; init; }

	/// <summary>
	/// Indicates whether metadata extraction is enabled.
	/// </summary>
	public bool EnableMetadata { get; init; }

	/// <summary>
	/// Indicates whether post-transfer verification is enabled.
	/// </summary>
	public bool EnableVerification { get; init; }

	/// <summary>
	/// Indicates whether original timestamps should be restored.
	/// </summary>
	public bool EnableTimestampCorrection { get; init; }


	// ---------------------------------------------------------------------
	// Execution hints
	// ---------------------------------------------------------------------

	/// <summary>
	/// Optional hint to limit parallel execution.
	/// A null value indicates that the engine may choose an appropriate default.
	/// </summary>
	public int? MaxDegreeOfParallelism { get; init; }
}
