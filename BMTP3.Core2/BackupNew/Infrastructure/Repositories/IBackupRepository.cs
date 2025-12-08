namespace BMTP3.Core2.BackupNew.Infrastructure.Repositories;

/// <summary>
/// Repository contract for persisting and retrieving backup resume records and their associated plan./// Repository for persisting and retrieving the state of backed-up items.
/// Repository for maintaining the history of processed items (Resume support)
/// Used as a persistent list of what has been done and history tracking.
/// </summary>
public interface IBackupRepository
{
	/// <summary>
	/// Load the persisted backup session (plan + records).
	/// </summary>
	Task<BackupSessionEntity?> LoadAsync();

	/// <summary>
	/// Save the backup session (plan + records).
	/// </summary>
	Task SaveAsync(BackupSessionEntity session);

	/// <summary>
	/// Delete the persisted session (optional).
	/// </summary>
	Task DeleteAsync();
}
