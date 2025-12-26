using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Hashing;
using BMTP3.Core2.BackupNew.Engine.Internal;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Tls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Engine.Steps.HashStep;

public class HashPipelineStage : AbstractPipelineStage<HashStepContext, HashStepResult>
{
	public HashPipelineStage(ILogger logger, int parallelism, HashStepContext context, List<IBackupItemStep<HashStepContext, HashStepResult>> steps, ProgressTracker tracker)
		: base(logger, parallelism, context, steps, tracker)
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
