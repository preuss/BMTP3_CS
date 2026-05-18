using BMTP3.Core4.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
	/// The moveable content (local temp file), set via <see cref="ReplaceContent"/>.
	/// Null until staging is complete. Read-only outside the record to prevent
	/// the two properties from drifting out of sync.
	/// </summary>
	public IMoveableContent? MoveableContent { get; private set; }

	/// <summary>
	/// Replaces both Item.Content and MoveableContent atomically.
	/// Use this after staging a file to local disk to ensure the item's
	/// content provider and the moveable reference stay in sync.
	/// </summary>
	/// <param name="moveableContent">The staged file content (local temp file).</param>
	public void ReplaceContent(IMoveableContent moveableContent)
	{
		Item.ReplaceContentProvider(moveableContent);
		MoveableContent = moveableContent;
	}
}
