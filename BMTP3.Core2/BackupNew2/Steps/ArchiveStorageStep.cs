using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BMTP3.Core2.BackupNew2.Interfaces;
using BMTP3.Core2.BackupNew2.Models;
using BMTP3.Core2.BackupNew2.Models.Configuration;

namespace BMTP3.Core2.BackupNew2.Steps;

public class ArchiveStorageStep : IBackupStep
{
    public string Name => "ArchiveStorage";

    public Task ExecuteAsync(IBackupItem item, BackupJob job, CancellationToken ct)
    {
        // Only process Copy or Rename actions
        if (item.Action != BackupActionType.Copy && item.Action != BackupActionType.Rename)
        {
            return Task.CompletedTask;
        }

        string targetPath = item.Metadata.Get<string>(MetadataKey.FinalTargetPath) ?? "";
        if (string.IsNullOrEmpty(targetPath))
        {
             item.Fail("Target path missing for Copy action", Name);
             return Task.CompletedTask;
        }

        if (job.DryRun)
        {
            // Log mock action?
            return Task.CompletedTask;
        }

        try 
        {
            string? dir = Path.GetDirectoryName(targetPath);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // Perform Copy
            // Prefer LocalTempPath for stability/speed
            if (item.Metadata.Has(MetadataKey.LocalTempPath))
            {
                string sourceFile = item.Metadata.Get<string>(MetadataKey.LocalTempPath)!;
                File.Copy(sourceFile, targetPath, overwrite: true);
            }
            else
            {
                // Fallback to stream copy
                using (var sourceStream = item.Content.OpenReadStream())
                using (var destStream = File.Create(targetPath))
                {
                    sourceStream.CopyTo(destStream);
                }
            }

            // Apply Timestamps
            if (item.Metadata.Has(MetadataKey.AuthoredDateTime))
            {
                DateTime dt = item.Metadata.Get<DateTime>(MetadataKey.AuthoredDateTime);
                File.SetCreationTimeUtc(targetPath, dt);
                File.SetLastWriteTimeUtc(targetPath, dt);
            }

            // Generate Sidecar
            if (job.SidecarFormat != SidecarFormat.None)
            {
                GenerateSidecar(item, targetPath, job.SidecarFormat);
            }

            item.State = BackupState.Completed;
        }
        catch (Exception ex)
        {
            item.Fail($"Copy failed: {ex.Message}", Name, ex);
        }

        return Task.CompletedTask;
    }

    private void GenerateSidecar(IBackupItem item, string targetFilePath, SidecarFormat format)
    {
        var data = new
        {
            OriginalFileName = item.Metadata.Get<string>(MetadataKey.OriginalFileName),
            OriginalSourceId = item.Metadata.Get<string>(MetadataKey.OriginalSourceId),
            OriginalPath = item.Content.OriginalPath,
            HashSha256 = item.Metadata.Get<string>(MetadataKey.HashSha256),
            AuthoredDate = item.Metadata.Get<DateTime>(MetadataKey.AuthoredDateTime),
            BackupDate = DateTime.UtcNow,
            SizeBytes = item.Metadata.Get<long>(MetadataKey.SizeBytes)
        };

        string sidecarPath = targetFilePath + (format == SidecarFormat.Json ? ".json" : ".ini");

        if (format == SidecarFormat.Json)
        {
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(sidecarPath, json);
        }
        else if (format == SidecarFormat.Ini)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[BackupMetadata]");
            sb.AppendLine($"OriginalFileName={data.OriginalFileName}");
            sb.AppendLine($"OriginalSourceId={data.OriginalSourceId}");
            sb.AppendLine($"OriginalPath={data.OriginalPath}");
            sb.AppendLine($"HashSha256={data.HashSha256}");
            sb.AppendLine($"AuthoredDate={data.AuthoredDate:O}"); // ISO 8601
            sb.AppendLine($"BackupDate={data.BackupDate:O}");
            sb.AppendLine($"SizeBytes={data.SizeBytes}");
            File.WriteAllText(sidecarPath, sb.ToString());
        }
    }
}
