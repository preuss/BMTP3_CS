using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Job;
/// <summary>
/// Snapshot of a backup job state transition.
/// </summary>
public class AuditJobEntry
{
	public JobState State { get; set; }
	public DateTime Timestamp { get; set; } = DateTime.UtcNow;

	/// <summary>
	/// Optional summary (e.g., "Job started", "Job completed with errors").
	/// </summary>
	public string? Summary { get; set; }

	/// <summary>
	/// Number of items in the job at this point.
	/// </summary>
	public int ItemCount { get; set; }
}