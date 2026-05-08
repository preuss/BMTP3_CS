using BMTP3.Core4.Models.Enums;

namespace BMTP3.Core4.Models;

/// <summary>
/// Represents a single item known to a backup session.
/// </summary>
internal sealed class BackupItem
{
	/// <summary>
	/// Unique identifier for this item within the backup session.
	/// </summary>
	public string Id { get; init; } = string.Empty;

	/// <summary>
	/// The original source path of the item.
	/// </summary>
	public string SourcePath { get; init; } = string.Empty;

	/// <summary>
	/// The path of the item relative to the configured backup source.
	/// </summary>
	public string RelativePath { get; init; } = string.Empty;

	/// <summary>
	/// The resolved destination path for this item.
	/// Null until the destination path has been determined.
	/// </summary>
	public string? DestinationPath { get; set; }

	/// <summary>
	/// The size of the item in bytes, if known.
	/// </summary>
	public long? SizeBytes { get; init; }

	/// <summary>
	/// The original last modified timestamp of the item, if known.
	/// </summary>
	public DateTimeOffset? ModifiedAt { get; init; }

	/// <summary>
	/// The current backup status of the item.
	/// </summary>
	public BackupItemStatus Status { get; set; } = BackupItemStatus.Pending;
}