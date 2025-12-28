using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BMTP3.Core2.BackupNew.Api.Request;

namespace BMTP3.Core2.BackupNew.Engine.Orchestration;

public interface IJobValidator
{
    Task ValidateAsync(BackupPlan plan, CancellationToken ct);
}

public class JobValidator : IJobValidator
{
    public Task ValidateAsync(BackupPlan plan, CancellationToken ct)
    {
        // 1. Validate Output Path Access and Space
        if (string.IsNullOrWhiteSpace(plan.OutputPath))
            throw new ArgumentException("Output path is required.");

        DirectoryInfo outputDir = new DirectoryInfo(plan.OutputPath);
        if (!outputDir.Exists)
        {
            // Try create to ensure access
            outputDir.Create(); 
        }

        // Check space (heuristic: warn if < 1GB, fail if < 10MB?)
        // Since we don't know total backup size yet (scanning hasn't happened),
        // we can only ensure we aren't completely full.
        long freeSpace = GetFreeSpace(outputDir.FullName);
        if (freeSpace < 50 * 1024 * 1024) // 50 MB safety buffer
        {
            throw new IOException($"Insufficient disk space on output drive '{outputDir.Root}'. Available: {freeSpace / 1024 / 1024} MB.");
        }

        return Task.CompletedTask;
    }

    private long GetFreeSpace(string path)
    {
        try
        {
            string root = Path.GetPathRoot(path) ?? path;
            DriveInfo drive = new DriveInfo(root);
            return drive.AvailableFreeSpace;
        }
        catch
        {
            // If we can't determine space (e.g. UNC path sometimes), assume OK or return max
            return long.MaxValue;
        }
    }
}
