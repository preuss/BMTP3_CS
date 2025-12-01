using System.Threading.Tasks;
using System;

namespace BMTP3.Core2.BackupNew2.Interfaces;

/// <summary>
/// Repository for persisting and retrieving the state of backed-up items.
/// Used for deduplication and history tracking.
/// </summary>
public interface IBackupStateRepository
{
    /// <summary>
    /// Loads the state from the storage (e.g., JSON file).
    /// </summary>
    Task LoadAsync();

    /// <summary>
    /// Saves the current in-memory state to storage.
    /// </summary>
    Task SaveAsync();

    /// <summary>
    /// Checks if a file with the given hash already exists in the repository.
    /// </summary>
    /// <param name="hash">The SHA-256 hash.</param>
    /// <returns>True if known, otherwise false.</returns>
    bool IsHashKnown(string hash);

    /// <summary>
    /// Checks if a file at the specific destination path already exists in the repository.
    /// </summary>
    bool IsPathKnown(string relativeDestinationPath);

    /// <summary>
    /// Registers a successfully backed up item.
    /// </summary>
    /// <param name="hash">File hash.</param>
    /// <param name="originalPath">Original source path.</param>
    /// <param name="destinationPath">Final destination path.</param>
    /// <param name="authoredDate">The determined authored date.</param>
    void RegisterItem(string hash, string originalPath, string destinationPath, DateTime authoredDate);
}
