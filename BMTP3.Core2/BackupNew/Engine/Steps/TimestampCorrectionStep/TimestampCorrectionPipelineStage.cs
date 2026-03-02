using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Internal;
using Microsoft.Extensions.Logging;

namespace BMTP3.Core2.BackupNew.Engine.Steps.TimestampCorrectionStep;

public class TimestampCorrectionPipelineStage : AbstractPipelineStage<BackupPlan>
{
	private readonly TimestampCorrectionItemStep _step;

	public TimestampCorrectionPipelineStage(
		ILogger logger,
		int parallelism,
		BackupPlan context,
		ProgressTracker tracker,
		TimestampCorrectionItemStep step)
		: base(logger, parallelism, context, tracker)
	{
		_step = step ?? throw new ArgumentNullException(nameof(step));
	}

	protected override async Task ProcessItemAsync(IBackupItem item, CancellationToken ct)
	{
		UpdatePhase(item, _step.Phase);
		var progress = CreateProgressReporter(item);
		await _step.ExecuteAsync(item, progress, ct).ConfigureAwait(false);
	}
}
