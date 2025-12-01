using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BMTP3.Core2.BackupNew2.Interfaces;
using BMTP3.Core2.BackupNew2.Models;
using BMTP3.Core2.BackupNew2.Models.Configuration;

namespace BMTP3.Core2.BackupNew2.Steps;

public class SourcePreparationStep : IBackupStep
{
    public string Name => "SourcePreparation";

    public async Task ExecuteAsync(IBackupItem item, BackupJob job, CancellationToken ct)
    {
        item.State = BackupState.Prepared;

        // Strategy:
        // 1. If MTP: Copy to a real local temp file to ensure stability and seekability.
        // 2. If FileSystem: Use the original path directly as "LocalTempPath" to avoid unnecessary IO,
        //    UNLESS we want to safeguard against network drive disconnection (future enhancement).
        
        if (job.SourceType == SourceType.Mtp)
        {
            string tempFile = Path.GetTempFileName();
            try 
            {
                using (var sourceStream = item.Content.OpenReadStream())
                using (var destStream = File.Create(tempFile))
                {
                    await sourceStream.CopyToAsync(destStream, ct);
                }

                item.Metadata.Set(MetadataKey.LocalTempPath, tempFile);
            }
            catch
            {
                // Cleanup on failure
                if (File.Exists(tempFile)) File.Delete(tempFile);
                throw;
            }
        }
        else
        {
            // For local files, the "temp" path is just the original path.
            // This tells subsequent steps "Read from here, it's safe and fast".
            // However, we must mark that this is NOT a temporary file to be deleted!
            // We can do this by NOT setting a "IsTempFile" flag (or assuming deletion only happens if we created it).
            // Actually, simpler: Set LocalTempPath. In Finalization, only delete if SourceType == Mtp.
            
            item.Metadata.Set(MetadataKey.LocalTempPath, item.Content.OriginalPath);
        }
    }
}
