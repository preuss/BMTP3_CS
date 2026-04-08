using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Internal;
using Microsoft.Extensions.Logging;

namespace BMTP3.Core2.BackupNew.Engine.Steps.MetadataExtractionStep;

public class MetadataExtractionPipelineStage : AbstractPipelineStage<BackupPlan>
{
	private readonly MetadataExtractionItemStep _step;

	public MetadataExtractionPipelineStage(
		ILogger logger,
		int parallelism,
		BackupPlan context,
		ProgressTracker tracker,
		MetadataExtractionItemStep step)
		: base(logger, parallelism, context, tracker)
	{
		_step = step ?? throw new ArgumentNullException(nameof(step));
	}

	protected override async Task ProcessItemAsync(IBackupItem item, CancellationToken ct)
	{
		UpdatePhase(item, _step.Phase);
		IProgress<ulong> progress = CreateProgressReporter(item);
		await _step.ExecuteAsync(item, progress, ct).ConfigureAwait(false);
	}
}