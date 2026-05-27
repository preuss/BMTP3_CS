using BMTP3.Core4.Models.Enums;
using System;

namespace BMTP3.Core4.Models;

internal sealed record BackupRecord
{
	/// <summary>
	/// The backup item associated with this record.
	/// </summary>
	public required BackupItem Item { get; init; }

	/// <summary>
	/// The resolved destination path for this item.
	/// Null until the destination path has been determined.
	/// </summary>
	public string? DestinationPath { get; set; }

	/// <summary>
	/// The current backup status of the item.
	/// </summary>
	public BackupItemStatus Status { get; set; } = BackupItemStatus.Pending;

	/// <summary>
	/// The timestamp when the status last changed.
	/// Null when the item is still in its initial Pending state.
	/// </summary>
	public DateTimeOffset? StatusChangedAt { get; set; }

	/// <summary>
	/// Item-level metadata (hashes, timestamps, etc.) accumulated during processing.
	/// </summary>
	public ItemMetadata Metadata { get; set; } = new();
}
