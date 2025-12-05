using BMTP3.Core2.BackupNew.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Job;
/// <summary>
/// Represents a backup job containing multiple items and audit history.
/// </summary>
public class BackupJob
{
	public JobState State { get; set; }

	/// <summary>
	/// Collection of items included in this job.
	/// </summary>
	public List<BackupItem> Items { get; set; } = new();

	/// <summary>
	/// Audit history of the job, recording errors, attempts, and stages.
	/// </summary>
	public List<AuditEntry> Audit { get; set; } = new();
}
