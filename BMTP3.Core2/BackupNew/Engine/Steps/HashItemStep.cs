using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Hashing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Engine.Steps;

public class HashItemStep : IBackupItemStep<BackupPlan, Dictionary<HashType, string>>
{
	public string Name => "Hashing";

	public Task<Dictionary<HashType, string>> ExecuteAsync(BackupPlan input, IBackupItem item, CancellationToken ct)
	{
		throw new NotImplementedException();
	}
}
