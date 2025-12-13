using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Hashing;
using Org.BouncyCastle.Tls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Engine.Steps;

public class HashStepWorkerPool : BackupStepWorkerPoolBase<BackupPlan, Dictionary<HashType, string>>
{
	/// <summary>
	/// Hook for subclasses to process the TResult returned by the step.
	/// Default implementation is a no-op.
	/// </summary>
	protected override Task OnResultAsync(BackupPlan context, IBackupItem item, Dictionary<HashType, string> result, CancellationToken ct)
	{
		return Task.CompletedTask;
	}
}
