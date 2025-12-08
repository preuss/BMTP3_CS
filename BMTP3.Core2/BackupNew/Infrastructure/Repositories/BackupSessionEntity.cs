using BMTP3.Core2.BackupNew.Api.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Infrastructure.Repositories;

/// <summary>
/// Aggregate entity representing a persisted backup session.
/// Contains the job plan and all item records for resume.
/// </summary>
public class BackupSessionEntity
{
	/// <summary>
	/// Unique identifier for the backup session/job.
	/// </summary>
	public required Guid SessionId { get; set; }

	/// <summary>
	/// The plan/configuration used to run this backup job.
	/// </summary>
	public required BackupPlan Plan { get; set; }

	/// <summary>
	/// All item records tracked for resume.
	/// </summary>
	public required List<BackupResumeRecord> Records { get; set; }
}