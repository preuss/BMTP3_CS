using BMTP3.Core4.Api.Models.Enums;

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
	// Source Definition
	// ---------------------------------------------------------------------

	/// <summary>
	/// The type of source to back up (e.g. filesystem or media device).
	/// </summary>
	public BackupSourceType SourceType { get; init; }

	/// <summary>
	/// The source path to back up.
	/// 
	/// Supports multiple source types:
	/// 
	/// <para><b>File system:</b> Absolute local or UNC path.</para>
	/// <para>Examples:</para>
	/// <para><c>C:\Users\John\Pictures</c></para>
	/// <para><c>\\NAS\Share\Backup</c></para>
	/// 
	/// <para><b>Media device (MTP/PTP):</b> URI formatted as:</para>
	/// <para><c>mtp://[Device Friendly Name]/[Storage or Root]/[Path]</c></para>
	/// 
	/// <para>The device name must match the name shown in Windows (e.g. "Apple iPad", "Canon Camera").</para>
	/// 
	/// <para>Examples:</para>
	/// <para><c>mtp://Apple iPad/Internal Storage/DCIM/202205__</c></para>
	/// <para><c>mtp://Canon Camera/SD Card/DCIM/100CANON</c></para>
	/// 
	/// <para>Notes:</para>
	/// <para>- "Internal Storage", "SD Card", etc. represent the device's root storage.</para>
	/// <para>- Most cameras store images under a <c>DCIM</c> folder.</para>
	/// <para>- Path segments use forward slashes (<c>/</c>).</para>
	/// </summary>
	public string SourcePath { get; init; } = string.Empty;

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
	public OutputStructureStrategy OutputStructureStrategy { get; init; }

	/// <summary>
	/// Custom template pattern for the output path.
	/// Only used when <see cref="OutputStructureStrategy"/> is <c>CustomPathPattern</c>.
	/// </summary>
	public string? CustomOutputPattern { get; init; }


	// ---------------------------------------------------------------------
	// Collision & Versioning Policy
	// ---------------------------------------------------------------------

	/// <summary>
	/// Defines how name collisions at the destination are handled.
	/// </summary>
	public CollisionStrategy CollisionStrategy { get; init; }

	/// <summary>
	/// Defines how files are compared to determine if a collision exists.
	/// </summary>
	public CollisionComparisonType CollisionComparisonType { get; init; }

	/// <summary>
	/// Defines how files are renamed when a collision is resolved by renaming.
	/// </summary>
	public RenameStrategy RenameStrategy { get; init; }

	/// <summary>
	/// Custom template pattern for renaming when <see cref="RenameStrategy"/> is <c>Custom</c>.
	/// </summary>
	public string? CustomOutputCollisionPattern { get; init; }


	// ---------------------------------------------------------------------
	// Metadata & Indexing
	// ---------------------------------------------------------------------

	/// <summary>
	/// The format used for sidecar files accompanying backed-up files.
	/// </summary>
	public SidecarFormat SidecarFormat { get; init; } = SidecarFormat.Ini;

	/// <summary>
	/// The format for the centralized backup index / catalog file.
	/// </summary>
	public BackupIndexType BackupIndexType { get; init; }

	/// <summary>
	/// The hash algorithms used when comparing files during collision detection.
	/// Only used when <see cref="CollisionComparisonType"/> is set to Hash.
	/// If null, a default set is chosen by the engine.
	/// </summary>
	public IReadOnlyList<HashAlgorithm>? ComparisonHashAlgorithms { get; init; }

	/// <summary>
	/// The hash algorithms used for post-write verification.
	/// Only used when <see cref="PostWriteVerification"/> is set to Hash.
	/// If null, a default set is chosen by the engine.
	/// </summary>
	public IReadOnlyList<HashAlgorithm>? VerificationHashAlgorithms { get; init; }

	/// <summary>
	/// Indicates whether metadata extraction is enabled.
	/// </summary>
	public bool EnableMetadata { get; init; }

	/// <summary>
	/// Specifies how files are verified after being written to the destination.
	/// </summary>
	public PostWriteVerificationType PostWriteVerification { get; init; }

	/// <summary>
	/// Indicates whether original timestamps should be restored.
	/// </summary>
	public bool EnableTimestampCorrection { get; init; }


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


	// ---------------------------------------------------------------------
	// Execution Hints
	// ---------------------------------------------------------------------

	/// <summary>
	/// Optional hint to limit parallel execution.
	/// A null value indicates that the engine may choose an appropriate default.
	/// </summary>
	public int? MaxDegreeOfParallelism { get; init; }
}
