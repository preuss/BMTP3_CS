namespace BMTP3.Core2.BackupNew2.Interfaces;

/// <summary>
/// Repository for persisting and retrieving the state of backed-up items.
/// Repository for maintaining the history of processed items (Resume support)
/// Used as a persistent list of what has been done and history tracking.
/// </summary>
public interface IBackupStateRepository
{
	/// <summary>
	/// Loads the state from the storage (e.g., JSON file) into memory.
	/// </summary>
	Task LoadAsync();

	/// <summary>
	/// Saves the current in-memory state to storage.
	/// </summary>
	Task SaveAsync();
}
