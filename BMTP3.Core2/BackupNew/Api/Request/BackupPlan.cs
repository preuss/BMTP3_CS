using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Engine.Hashing;

namespace BMTP3.Core2.BackupNew.Api.Request;

/// <summary>
/// Represents a single, fully configured backup job to be executed by the BackupEngine.
/// This acts as the DTO between the Console/UI layer and the Core Logic.
/// </summary>
public class BackupPlan
{
	// --------------------------------------------------
	// 1. IDENTIFICATION
	// --------------------------------------------------

	/// <summary>
	/// Friendly name for the job (e.g., "iPhone Photos", "C-Drive Docs").
	/// Used for logging and display.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	// --------------------------------------------------
	// 2. SOURCE DEFINITION
	// --------------------------------------------------

	/// <summary>
	/// The type of source (FileSystem vs. MTP Device).
	/// </summary>
	public SourceType SourceType { get; set; }

	/// <summary>
	/// The unique identifier for the source. This could be a device ID, a drive letter, or a network path.
	/// <para>FileSystem: Drive letter or root path (e.g., "C:", "\\Nas\Share").</para>
	/// <para>MTP: The friendly name of the device (e.g., "Apple iPhone", "Galaxy S21").</para>
	/// </summary>
	public string SourceId { get; set; } = string.Empty; // CHANGED FROM SourceDeviceId

	/// <summary>
	/// The specific path within the device to back up.
	/// <para>FileSystem: Absolute path (e.g., "Users\John\Pictures").</para>
	/// <para>MTP: Path relative to device root (e.g., "\Internal Storage\DCIM").</para>
	/// </summary>
	public string SourcePath { get; set; } = string.Empty;

	// --------------------------------------------------
	// 3. DESTINATION
	// --------------------------------------------------

	/// <summary>
	/// The root folder where backups will be stored.
	/// </summary>
	public string OutputPath { get; set; } = string.Empty;

	// --------------------------------------------------
	// 4. SCOPE & FILTERING
	// --------------------------------------------------

	/// <summary>
	/// Whether to traverse subdirectories.
	/// </summary>
	public bool Recursive { get; set; } = true;

	/// <summary>
	/// Glob patterns to include (e.g., "**/*.jpg"). Empty = Include All.
	/// </summary>
	public List<string> IncludePatterns { get; set; } = new();

	/// <summary>
	/// Glob patterns to exclude (e.g., "**/.git", "**/*.tmp").
	/// </summary>
	public List<string> ExcludePatterns { get; set; } = new();

	// --------------------------------------------------
	// 5. OUTPUT STRUCTURE & NAMING
	// --------------------------------------------------

	/// <summary>
	/// Strategy for folder structure (Preserve vs Flat vs Custom).
	/// </summary>
	public OutputStructureStrategy OutputStrategy { get; set; } = OutputStructureStrategy.PreserveSourceTree; // UPDATED DEFAULT

	/// <summary>
	/// Template pattern for output path if OutputStrategy is CustomPathPattern.
	/// Supports variables: ${yyyy}, ${MM}, ${originalName}, etc.
	/// </summary>
	public string? CustomOutputPathPattern { get; set; }

	// --------------------------------------------------
	// 6. COLLISION & VERSIONING POLICY
	// --------------------------------------------------

	/// <summary>
	/// How to compare files before deciding on a collision (None, Hash, Binary).
	/// Default is Binary for maximum data integrity.
	/// </summary>
	public CollisionComparisonType ComparisonType { get; set; } = CollisionComparisonType.Binary;  // UPDATED DEFAULT

	/// <summary>
	/// Action to take if a collision is confirmed (Overwrite, Skip, Rename).
	/// Default is Rename to prevent data loss and ensure process completion.
	/// </summary>
	public CollisionResolutionType CollisionResolution { get; set; } = CollisionResolutionType.Rename; // UPDATED DEFAULT

	/// <summary>
	/// Strategy for renaming if CollisionResolution is 'Rename'.
	/// Default is Increment for simple versioning.
	/// </summary>
	public RenameStrategy RenameStrategy { get; set; } = RenameStrategy.Increment; // UPDATED DEFAULT and not nullable

	/// <summary>
	/// Template pattern for renaming if RenameStrategy is CustomCollisionPathPattern.
	/// </summary>
	public string? CustomCollisionPathPattern { get; set; }

	// --------------------------------------------------
	// 7. METADATA & LOGGING
	// --------------------------------------------------

	/// <summary>
	/// Format for per-file metadata sidecars.
	/// Default is Ini for human-readable metadata.
	/// </summary>
	public SidecarFormat SidecarFormat { get; set; } = SidecarFormat.Ini; // UPDATED DEFAULT

	/// <summary>
	/// Format for centralized backup index/catalog.
	/// Default is Json for easy inspection and versioning.
	/// </summary>
	public BackupIndexType BackupIndexType { get; set; } = BackupIndexType.Json; // UPDATED DEFAULT

	public ISet<HashType> HashTypes { get; set; } = new HashSet<HashType> {
		HashType.SHA3_512_KECCAK,
		HashType.SHA3_512_FIPS202,
		HashType.SHA2_512,
		HashType.SHA2_256,
		HashType.MD5_128,
		HashType.BLAKE3_256,
		HashType.BLAKE3_512
	}; // UPDATED DEFAULT

	// --------------------------------------------------
	// 8. EXECUTION CONTROL
	// --------------------------------------------------

	/// <summary>
	/// If true, calculates paths and decisions but performs no I/O (Write/Delete).
	/// </summary>
	public bool DryRun { get; set; } = false;

	/// <summary>
	/// Artificial delay in ms between items (for throttling).
	/// </summary>
	public int DelayMs { get; set; } = 0;
}
