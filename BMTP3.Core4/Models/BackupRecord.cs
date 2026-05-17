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
	/// The moveable content, set after the item has been staged to a local file.
	/// Null until staging is complete.
	/// </summary>
	public IMoveableContent? MoveableContent { get; set; }
}
