using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Hashing;
using BMTP3.Core2.BackupNew.Engine.Internal; // Added for ProgressTracker
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Tls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Engine.Steps.HashStep;

public class HashPipelineStageForAbstract : AbstractPipelineStage<HashStepContext, HashStepResult> // Correct base class
{
	public HashPipelineStageForAbstract(ILogger logger, int parallelism, HashStepContext context, IBackupItemStep<HashStepContext, HashStepResult> step, ProgressTracker tracker)
		: base(logger, parallelism, context, step, tracker)
	{
	}

	protected override Task OnResultAsync(IBackupItem item, HashStepResult result, CancellationToken ct)
	{
		return Task.CompletedTask;
	}
}
