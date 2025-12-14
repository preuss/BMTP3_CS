using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Hashing;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Tls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Engine.Steps.HashStep;

public class HashStepWorkerPool : BackupStepWorkerPoolBase<HashStepContext, HashStepResult>
{
	public HashStepWorkerPool(ILogger logger, int parallelism, HashStepContext context, IBackupItemStep<HashStepContext, HashStepResult> step)
		: base(logger, parallelism, context, step)
	{
	}

	/// <summary>
	/// Hook for subclasses to process the TResult returned by the step.
	/// Default implementation is a no-op.
	/// </summary>
	protected override Task OnResultAsync(IBackupItem item, HashStepResult result, CancellationToken ct)
	{
		return Task.CompletedTask;
	}
}
