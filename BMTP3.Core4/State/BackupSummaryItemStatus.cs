using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core4.State;

/// <summary>
/// Persisted status of a backup item in a summary file.
/// Failed items are not persisted — they are retried on resume and therefore treated as Pending.
/// </summary>
public enum BackupSummaryItemStatus
{
	/// <summary>The item has not yet been processed, or failed and will be retried.</summary>
	Pending,

	/// <summary>The item was successfully backed up.</summary>
	Succeeded,

	/// <summary>The item was intentionally skipped and requires no further processing.</summary>
	Skipped,
}
