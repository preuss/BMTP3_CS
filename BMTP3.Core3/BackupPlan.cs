namespace BMTP3.Core3;

/// <summary>
/// Input configuration for a backup job.
/// </summary>
public class BackupPlan
{
	/// <summary>
	/// Source directory or device path.
	/// </summary>
	public required string Source { get; init; }

	/// <summary>
	/// Destination directory where files will be copied.
	/// </summary>
	public required string Destination { get; init; }

	/// <summary>
	/// If true, simulate backup without actually writing files.
	/// Useful for testing backup plans without data changes.
	/// </summary>
	public bool DryRun { get; init; }

	/// <summary>
	/// Strategy for handling naming conflicts at destination.
	/// </summary>
	public CollisionStrategy Collision { get; init; } = CollisionStrategy.Rename;

	/// <summary>
	/// List of hash algorithms to compute for each file.
	/// Computed in a single pass through the file.
	/// Defaults to SHA2_256, SHA3_256_FIPS202, BLAKE3_256 if not specified.
	/// </summary>
	public List<HashType> HashTypes { get; init; } = new()
	{
		HashType.SHA2_256,
		HashType.SHA3_256_FIPS202,
		HashType.BLAKE3_256
	};
}

/// <summary>
/// Strategy for handling filename conflicts during backup.
/// </summary>
public enum CollisionStrategy
{
	/// Rename conflicting file (file.jpg → file_1.jpg, file_2.jpg, etc.)
	Rename = 0,

	/// Skip conflicting file and continue backup
	Skip = 1,

	/// Overwrite existing destination file
	Overwrite = 2,

	/// Treat as error and stop backup
	Error = 3
}

/// <summary>
/// Hash algorithm types. All are computed in a single pass for efficiency.
/// </summary>
public enum HashType
{
	SHA2_256 = 0,
	SHA2_512 = 1,
	SHA3_256_FIPS202 = 2,
	SHA3_512_FIPS202 = 3,
	SHA3_256_KECCAK = 4,
	SHA3_512_KECCAK = 5,
	MD5_128 = 6,
	BLAKE3_256 = 7,
	BLAKE3_512 = 8
}
