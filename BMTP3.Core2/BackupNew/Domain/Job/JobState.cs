using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Domain.Job;
/// <summary>
/// Represents the overall lifecycle state of a backup job.
/// </summary>
public enum JobState
{
	/// <summary>
	/// Job has been created and is ready to start.
	/// </summary>
	Ready,

	/// <summary>
	/// Job is currently running.
	/// </summary>
	Running,

	/// <summary>
	/// Job finished successfully.
	/// </summary>
	Completed,

	/// <summary>
	/// Job finished with errors and did not succeed.
	/// </summary>
	Failed,

	/// <summary>
	/// Job was intentionally cancelled before completion (can be resumed later).
	/// </summary>
	Cancelled
}
