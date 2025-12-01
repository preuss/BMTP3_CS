using System.Threading;
using System.Threading.Tasks;
using BMTP3.Core2.BackupNew2.Interfaces;
using BMTP3.Core2.BackupNew2.Models;
using BMTP3.Core2.BackupNew2.Models.Configuration;

namespace BMTP3.Core2.BackupNew2.Steps;

public class FinalizationStep : IBackupStep
{
    public string Name => "Finalization";

    public FinalizationStep()
    {
        // No dependencies needed anymore
    }

    public Task ExecuteAsync(IBackupItem item, BackupJob job, CancellationToken ct)
    {
        item.State = BackupState.Finalized;

        // Cleanup temp files
        if (job.SourceType == SourceType.MediaDevice && item.Metadata.Has(MetadataKey.LocalTempPath))
        {
            string tempPath = item.Metadata.Get<string>(MetadataKey.LocalTempPath)!;
            if (System.IO.File.Exists(tempPath))
            {
                try
                {
                    System.IO.File.Delete(tempPath);
                }
                catch
                {
                    // Swallow cleanup errors, they shouldn't fail the job
                }
            }
        }
        
        return Task.CompletedTask;
    }
}
