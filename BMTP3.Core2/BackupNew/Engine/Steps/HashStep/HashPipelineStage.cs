using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Internal;
using Microsoft.Extensions.Logging;

namespace BMTP3.Core2.BackupNew.Engine.Steps.HashStep;

public class HashPipelineStage : AbstractPipelineStage<HashStepContext>
{
	private readonly HashItemStep _hashStep;

	public HashPipelineStage(
		ILogger logger,
		int parallelism,
		HashStepContext context,
		ProgressTracker tracker,
		HashItemStep hashStep)
		: base(logger, parallelism, context, tracker)
	{
		_hashStep = hashStep ?? throw new ArgumentNullException(nameof(hashStep));
	}

	protected override async Task ProcessItemAsync(IBackupItem item, CancellationToken ct)
	{
		// 1. Tell tracker we are Hashing
		UpdatePhase(item, _hashStep.Phase);

		// 2. Create reporter for bytes
		var progress = CreateProgressReporter(item);

		// 3. Execute step with progress
		await _hashStep.ExecuteAsync(item, progress, ct).ConfigureAwait(false);
	}
}
