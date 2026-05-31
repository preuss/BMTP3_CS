using BMTP3.Core4.Models;

namespace BMTP3.Core4.Engine.Session;

/// <summary>
/// Provides an in-session store for <see cref="BackupRecord"/> instances,
/// tracking every item that has been processed during a single backup run.
/// </summary>
internal interface IBackupRecordRepository
{
	/// <summary>
	/// Adds a <see cref="BackupRecord"/> to the repository.
	/// </summary>
	/// <param name="record">The record to add.</param>
	void Add(BackupRecord record);

	/// <summary>
	/// Returns all <see cref="BackupRecord"/> instances currently held in the repository.
	/// </summary>
	/// <returns>A list of all backup records.</returns>
	List<BackupRecord> GetAll();
}
