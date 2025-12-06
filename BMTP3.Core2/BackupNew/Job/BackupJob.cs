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
	public Guid JobId { get; }
	public string? Name { get; }

	/// <summary>
	/// Current lifecycle state of the job.
	/// </summary>
	public JobState State { get; private set; }

	/// <summary>
	/// Collection of items included in this job.
	/// </summary>
	public List<BackupItem> Items { get; }

	private readonly List<AuditJobEntry> _auditTrail = new();
	/// <summary>
	/// Audit trail of job state transitions (job-level only).
	/// </summary>
	public IReadOnlyList<AuditJobEntry> AuditTrail => _auditTrail;

	/// <summary>
	/// Creates a new backup job with an initial state and optional items.
	/// </summary>
	public BackupJob(string? name = null, IEnumerable<BackupItem>? items = null)
	{
		JobId = Guid.NewGuid();
		Name = name ?? JobId.ToString("b");

		State = JobState.Ready;
		Items = new();
		_auditTrail = new();

		if(items != null)
		{
			Items.AddRange(items);
		}

		AddAudit(JobState.Ready, "Job created");
	}
	internal void AddAudit(JobState state, string summary)
	{
		_auditTrail.Add(new AuditJobEntry
		{
			State = state,
			Timestamp = DateTime.UtcNow,
			Summary = summary,
			ItemCount = Items.Count
		});
	}
}