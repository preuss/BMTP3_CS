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
	/// <summary>
	/// Current lifecycle state of the job.
	/// </summary>
	public JobState State { get; set; } = JobState.Ready;

	/// <summary>
	/// Collection of items included in this job.
	/// </summary>
	public List<BackupItem> Items { get; } = new();

	/// <summary>
	/// Audit trail of the job, recording state transitions and attempts.
	/// </summary>
	public List<AuditEntry> AuditTrail { get; } = new();
}