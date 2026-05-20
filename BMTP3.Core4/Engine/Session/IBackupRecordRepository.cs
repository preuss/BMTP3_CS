using BMTP3.Core4.Models;
using BMTP3.Core4.Models.Enums;

namespace BMTP3.Core4.Engine.Session;

internal interface IBackupRecordRepository
{
	void Add(BackupRecord record);
}
