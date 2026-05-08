using BMTP3.Core4.Models;
using BMTP3.Core4.Models.Enums;

namespace BMTP3.Core4.Engine.State;

/// <summary>
/// Represents the internal runtime state of a single backup execution.
/// This state is the single source of truth for what the backup engine
/// knows about the current session.
/// </summary>
internal sealed class BackupSessionState
{
	public BackupSessionState(BackupPlan plan)
	{
		Plan = plan ?? throw new ArgumentNullException(nameof(plan));
		Phase = BackupPhase.Starting;
	}

	/// <summary>
	/// The immutable backup plan for this session.
	/// </summary>
	public BackupPlan Plan { get; }

	/// <summary>
	/// The current high-level phase of the backup job.
	/// </summary>
	public BackupPhase Phase { get; set; }

	/// <summary>
	/// All items known to the backup session.
	/// This collection represents the complete scope of the backup.
	/// </summary>
	public IList<BackupItem> Items { get; } = new List<BackupItem>();

	/// <summary>
	/// Optional terminal failure reason if the backup ends in Failed state.
	/// </summary>
	public BackupErrorCode? FailureReason { get; set; }
}