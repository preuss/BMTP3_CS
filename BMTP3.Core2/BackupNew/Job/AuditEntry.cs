using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Job;
/// <summary>
/// Represents a single audit entry for a backup job.
/// </summary>
public class AuditEntry
{
	/// <summary>
	/// Stage of the job when this entry was recorded (e.g., Copy, Verify).
	/// </summary>
	public string Stage { get; set; }

	/// <summary>
	/// Error message if the stage failed, otherwise null.
	/// </summary>
	public string? Error { get; set; }

	/// <summary>
	/// Number of attempts made at this stage.
	/// </summary>
	public uint AttemptCount { get; set; }

	/// <summary>
	/// Timestamp of when this entry was recorded.
	/// </summary>
	public DateTime Timestamp { get; set; }

	// TODO: Additional fields like Duration, ItemCount, etc. can be added as needed.
	// TODO: We can expand later example with: WorkerId, Duration, ResultState.
}
