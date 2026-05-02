namespace BMTP3.Core3;

/// <summary>
/// Represents a single file or folder item in the backup.
/// Immutable data structure that flows through the backup pipeline.
/// Each step transforms items by adding state (hashes, metadata, etc.).
/// </summary>
public record BackupItem
{
	/// <summary>
	/// Name of the item (filename or folder name).
	/// </summary>
	public required string Name { get; init; }

	/// <summary>
	/// Full path to the item in the source.
	/// </summary>
	public required string SourcePath { get; init; }

	/// <summary>
	/// Full path to where the item will be (or was) written.
	/// Updated by collision resolution if needed.
	/// </summary>
	public required string DestinationPath { get; init; }

	/// <summary>
	/// Type of item: File or Folder.
	/// </summary>
	public BackupItemType Type { get; init; } = BackupItemType.File;

	/// <summary>
	/// Size in bytes.
	/// </summary>
	public long SizeInBytes { get; init; }

	/// <summary>
	/// When the item was originally created (on source).
	/// </summary>
	public DateTime CreatedAt { get; init; }

	/// <summary>
	/// When the item was last modified (on source).
	/// </summary>
	public DateTime ModifiedAt { get; init; }

	/// <summary>
	/// Optional metadata extracted from the file (timestamps, EXIF, etc.).
	/// Added by MetadataExtractionStep.
	/// </summary>
	public Dictionary<string, object>? Metadata { get; init; }

	/// <summary>
	/// Hash values computed for this item.
	/// Maps HashType to hex string representation.
	/// Added by GenerateHashesStep.
	/// </summary>
	public Dictionary<HashType, string>? Hashes { get; init; }

	/// <summary>
	/// Path to the sidecar file if generated.
	/// </summary>
	public string? SidecarPath { get; init; }

	/// <summary>
	/// Result state of this item after processing.
	/// </summary>
	public BackupItemResultState ResultState { get; init; } = BackupItemResultState.Pending;

	/// <summary>
	/// Helper: create a new BackupItem with resolved destination path (for collision handling).
	/// </summary>
	public BackupItem WithDestinationPath(string newPath) => this with { DestinationPath = newPath };

	/// <summary>
	/// Helper: create a new BackupItem with metadata.
	/// </summary>
	public BackupItem WithMetadata(Dictionary<string, object> metadata) => this with { Metadata = metadata };

	/// <summary>
	/// Helper: create a new BackupItem with hashes.
	/// </summary>
	public BackupItem WithHashes(Dictionary<HashType, string> hashes) => this with { Hashes = hashes };

	/// <summary>
	/// Helper: create a new BackupItem with sidecar path.
	/// </summary>
	public BackupItem WithSidecarPath(string sidecarPath) => this with { SidecarPath = sidecarPath };

	/// <summary>
	/// Helper: create a new BackupItem marked as successful.
	/// </summary>
	public BackupItem WithSuccess() =>
		this with { ResultState = BackupItemResultState.Success };

	/// <summary>
	/// Helper: create a new BackupItem marked as failed.
	/// </summary>
	public BackupItem WithFailure() =>
		this with { ResultState = BackupItemResultState.Failed };
}

/// <summary>
/// Type of backup item.
/// </summary>
public enum BackupItemType
{
	File = 0,
	Folder = 1
}

/// <summary>
/// Result state of a backup item.
/// </summary>
public enum BackupItemResultState
{
	Pending = 0,
	Success = 1,
	Failed = 2
}
