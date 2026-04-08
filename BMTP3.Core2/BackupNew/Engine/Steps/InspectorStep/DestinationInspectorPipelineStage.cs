using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Internal;
using Microsoft.Extensions.Logging;

namespace BMTP3.Core2.BackupNew.Engine.Steps.InspectorStep;

public class DestinationInspectorPipelineStage : AbstractPipelineStage<BackupPlan>
{
	private readonly DestinationInspectorItemStep _step;

	public DestinationInspectorPipelineStage(ILogger logger, int parallelism, BackupPlan context,
		ProgressTracker tracker, DestinationInspectorItemStep step)
		: base(logger, parallelism, context, tracker)
	{
		_step = step ?? throw new ArgumentNullException(nameof(step));
	}

	protected override async Task ProcessItemAsync(IBackupItem item, CancellationToken ct)
	{
		IProgress<ulong> progress = CreateProgressReporter(item);
		UpdatePhase(item, _step.Phase);
		await _step.ExecuteAsync(item, progress, ct).ConfigureAwait(false);
	}
}