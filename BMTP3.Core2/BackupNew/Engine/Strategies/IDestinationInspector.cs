namespace BMTP3.Core2.BackupNew.Engine.Strategies;

/// <summary>
///     Provides information and cached operations for files at the destination.
///     Ensures per-path synchronization for hash computation and inspection.
/// </summary>
public interface IDestinationInspector
{
	/// <summary>
	///     Returns a lightweight snapshot for the given path (exists, length, last write).
	/// </summary>
	Task<FileSnapshot> GetSnapshotAsync(string path, CancellationToken ct);

	/// <summary>
	///     Returns a hex-encoded hash for the destination file using the requested algorithm (e.g. "SHA256").
	///     Implementations SHOULD cache results and compute under a per-path lock to avoid duplicate hashing.
	/// </summary>
	Task<string> GetHashAsync(string path, string algorithm, CancellationToken ct);
}

/// <summary>
///     Lightweight snapshot of a file on disk used for quick comparisons.
/// </summary>
public sealed class FileSnapshot
{
	public bool Exists { get; init; }
	public ulong Length { get; init; }
	public DateTime LastWriteTimeUtc { get; init; }
}