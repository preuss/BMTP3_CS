using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Infrastructure.Repositories;

namespace BMTP3.Core2.BackupNew.Domain.Repositories;

/// <summary>
/// Repository contract for persisting and retrieving backup resume records and their associated plan.
/// Repository for persisting and retrieving the state of backed-up items.
/// Repository for maintaining the history of processed items (Resume support)
/// Used as a persistent list of what has been done and history tracking.
/// </summary>
public interface IBackupRepository
{
	/// <summary>
	/// Load the persisted backup session (plan + records).
	/// </summary>
	Task<BackupSessionEntity?> LoadAsync(CancellationToken ct = default);

	/// <summary>
	/// Save the backup session (plan + records).
	/// </summary>
	Task SaveAsync(BackupSessionEntity session, CancellationToken ct = default);

	/// <summary>
	/// Delete the persisted session (optional).
	/// </summary>
	Task DeleteAsync(CancellationToken ct = default);

	/// <summary>
	/// Persist the state of a single backup item (for diagnostics and resume support).
	/// </summary>
	Task PersistItemStateAsync(IBackupItem item, CancellationToken ct = default);
}
