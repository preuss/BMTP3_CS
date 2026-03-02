using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Internal;
using Microsoft.Extensions.Logging;

namespace BMTP3.Core2.BackupNew.Engine.Steps.StagingStep;

public class ContentBufferingPipelineStage : AbstractPipelineStage<BackupPlan>
{
	private readonly ContentBufferingItemStep _step;

	public ContentBufferingPipelineStage(
		ILogger logger,
		int parallelism,
		BackupPlan context,
		ProgressTracker tracker,
		ContentBufferingItemStep step)
		: base(logger, parallelism, context, tracker)
	{
		_step = step ?? throw new ArgumentNullException(nameof(step));
	}

	protected override async Task ProcessItemAsync(IBackupItem item, CancellationToken ct)
	{
		// Ensure the step has the pipeline context before executing.
		_step.Context = Context;

		UpdatePhase(item, _step.Phase);
		IProgress<ulong> progress = CreateProgressReporter(item);
		await _step.ExecuteAsync(item, progress, ct).ConfigureAwait(false);
	}
}
