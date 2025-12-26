using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Internal;
using BMTP3.Core2.BackupNew.Engine.Models;
using Microsoft.Extensions.Logging;
using BMTP3.Core2.BackupNew.Api.Enums; // For BackupPhase

namespace BMTP3.Core2.BackupNew.Engine.Steps.TransferStep;

public class TransferPipelineStage : AbstractPipelineStage<BackupPlan, OperationResult>
{
    public TransferPipelineStage(
        ILogger logger, 
        int parallelism, 
        BackupPlan context, 
        IBackupItemStep<BackupPlan, OperationResult> step, 
        ProgressTracker tracker)
        : base(logger, parallelism, context, step, tracker)
    {
    }
}
