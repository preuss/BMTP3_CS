using BMTP3.Core4.Models.Enums;

namespace BMTP3.Core4.Models;

/// <summary>
/// Represents an internal Core4 model describing a single item
/// participating in a backup job.
/// </summary>
internal interface IBackupItem
{
	/// <summary>
	/// Unique identifier for this item within the current backup job.
	/// </summary>
	string Id { get; }

	/// <summary>
	/// The original source path of the item.
	/// </summary>
	string SourcePath { get; }

	/// <summary>
	/// The path of the item relative to the configured backup source.
	/// </summary>
	string RelativePath { get; }

	/// <summary>
	/// The resolved destination path for this item.
	/// Null until the destination path has been determined.
	/// </summary>
	string? DestinationPath { get; set; }

	/// <summary>
	/// The size of the item in bytes, if known.
	/// </summary>
	long? SizeBytes { get; }

	/// <summary>
	/// The original last modified timestamp of the item, if known.
	/// </summary>
	DateTimeOffset? ModifiedAt { get; }

	/// <summary>
	/// The current processing status of the item.
	/// </summary>
	BackupItemStatus Status { get; set; }
}