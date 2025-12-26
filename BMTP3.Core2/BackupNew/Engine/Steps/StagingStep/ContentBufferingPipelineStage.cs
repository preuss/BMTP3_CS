using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Internal;
using Microsoft.Extensions.Logging;
using BMTP3.Core2.BackupNew.Api.Enums; // For BackupPhase

namespace BMTP3.Core2.BackupNew.Engine.Steps.StagingStep;

public class ContentBufferingPipelineStage : AbstractPipelineStage<BackupPlan, bool>
{
    public ContentBufferingPipelineStage(
        ILogger logger, 
        int parallelism, 
        BackupPlan context, 
        List<IBackupItemStep<BackupPlan, bool>> steps, 
        ProgressTracker tracker)
        : base(logger, parallelism, context, steps, tracker)
    {
    }
}
