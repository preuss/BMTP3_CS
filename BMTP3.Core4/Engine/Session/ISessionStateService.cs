using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Models;

namespace BMTP3.Core4.Engine.Session;

/// <summary>
/// Manages persisting and restoring backup session state.
/// The service owns the translation between in-memory <see cref="BackupRecord"/>
/// and persisted <see cref="BackupSummary"/> / <see cref="BackupSummaryItem"/>.
/// Callers never interact with the summary store directly.
/// </summary>
internal interface ISessionStateService
{
	/// <summary>
	/// Loads the persisted summary for the given session and applies its state
	/// (DestinationPath, Status, StatusChangedAt) onto the provided records.
	/// If no persisted summary exists the call is a no-op.
	/// </summary>
	/// <param name="records">All records in the current session, already scanned.</param>
	/// <param name="sessionKey">Identifies which session to resume.</param>
	/// <param name="resumeBehavior">Controls behaviour when the scanned file list differs from the persisted summary.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <exception cref="SessionResumeMismatchException">
	/// Thrown when <paramref name="resumeBehavior"/> is <see cref="SessionResumeStrategy.Abort"/>
	/// and the scanned file list differs from the persisted summary.
	/// </exception>
	Task ApplyResumeAsync(
		IReadOnlyList<BackupRecord> records,
		BackupSessionKey sessionKey,
		SessionResumeStrategy resumeBehavior,
		CancellationToken cancellationToken
	);

	/// <summary>
	/// Persists the current state of all records as a session summary.
	/// </summary>
	/// <param name="records">All records whose state should be persisted.</param>
	/// <param name="sessionKey">Identifies the session.</param>
	Task SaveAsync(
		IReadOnlyList<BackupRecord> records,
		BackupSessionKey sessionKey
	);

	/// <summary>
	/// Deletes the persisted summary for the current session, if any.
	/// </summary>
	Task DeleteAsync();
}