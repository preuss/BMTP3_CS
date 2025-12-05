using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Job;
/// <summary>
/// Represents the final outcome of a backup attempt for a single item.
/// </summary>
public enum ResultState
{
	/// <summary>
	/// Item is registered and waiting to be processed.
	/// </summary>
	Pending,

	/// <summary>
	/// Item backup completed successfully.
	/// </summary>
	Success,

	/// <summary>
	/// Item backup attempt failed.
	/// </summary>
	Failed,

	/// <summary>
	/// Item was intentionally skipped (e.g., file already exists).
	/// </summary>
	Skipped
}
