using BMTP3.Core4.Models;

namespace BMTP3.Core4.Engine.Session;
internal class BackupMemoryRecordRepository : IBackupRecordRepository
{
	private readonly List<BackupRecord> _records = new();

	public void Add(BackupRecord record)
	{
		ArgumentNullException.ThrowIfNull(record);

		if (_records.Any(existing => existing.Item.Id == record.Item.Id))
		{
			throw new InvalidOperationException($"A backup record with id '{record.Item.Id}' already exists.");
		}

		_records.Add(record);
	}

	public List<BackupRecord> GetAll()
	{
		return _records.ToList();
	}
}