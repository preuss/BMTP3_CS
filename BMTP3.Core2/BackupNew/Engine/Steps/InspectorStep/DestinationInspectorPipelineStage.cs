using BMTP3.Core2.BackupNew.Domain.Item;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;
using BMTP3.Core2.BackupNew.Engine.Internal;
using BMTP3.Core2.BackupNew.Api.Request;

namespace BMTP3.Core2.BackupNew.Engine.Steps.InspectorStep;

public class DestinationInspectorPipelineStage : AbstractPipelineStage<BackupPlan>
{
    private readonly DestinationInspectorItemStep _step;

    public DestinationInspectorPipelineStage(ILogger logger, int parallelism, BackupPlan context, ProgressTracker tracker, DestinationInspectorItemStep step)
        : base(logger, parallelism, context, tracker)
    {
        _step = step ?? throw new ArgumentNullException(nameof(step));
    }

    protected override async Task ProcessItemAsync(IBackupItem item, CancellationToken ct)
    {
        var progress = CreateProgressReporter(item);
        UpdatePhase(item, _step.Phase);
        await _step.ExecuteAsync(item, progress, ct).ConfigureAwait(false);
    }
}
