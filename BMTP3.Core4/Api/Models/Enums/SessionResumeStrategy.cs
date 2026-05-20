namespace BMTP3.Core4.Api.Models.Enums;
/// <summary>
/// Defines how a backup session should behave when attempting to resume
/// from a previously persisted session state.
///
/// This strategy is applied when a prior session exists and the current
/// source file list does not perfectly match the stored session data
/// (e.g. added, removed, or changed files).
///
/// It determines whether to abort, continue with reconciliation, or
/// discard the previous session and start over.
/// </summary>
public enum SessionResumeStrategy
{
	/// <summary>
	/// Abort the operation immediately if any mismatch or inconsistency
	/// is detected between the current source state and the persisted session.
	///
	/// Use this to ensure strict consistency and avoid any risk of incorrect backups.
	/// </summary>
	Abort,

	/// <summary>
	/// Continue the backup by reconciling the existing session with the current source state.
	///
	/// Behavior:
	/// - Retains progress for files that still exist and were previously completed.
	/// - Keeps incomplete files as pending.
	/// - Adds new files as pending.
	/// - Removes any files that no longer exist in the source.
	///
	/// This results in a normalized session representing the current source state.
	/// </summary>
	Continue,

	/// <summary>
	/// Discard the existing session entirely and start a new backup from scratch.
	///
	/// All previous progress is lost, and a new session is created based solely
	/// on the current source file list, with all files marked as pending.
	/// </summary>
	Restart
}
