using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using BMTP3.Core2.BackupNew2.Interfaces;
using BMTP3.Core2.BackupNew2.Models;
using BMTP3.Core2.BackupNew2.Models.Configuration;

namespace BMTP3.Core2.BackupNew2.Steps;

public class FileAnalysisStep : IBackupStep
{
    public string Name => "FileAnalysis";

    public async Task ExecuteAsync(IBackupItem item, BackupJob jobConfig, CancellationToken ct)
    {
        item.State = BackupState.Analyzed; // Optimistic start

        // Use the prepared local path if available (Preferred for performance/stability)
        Stream stream;
        if (item.Metadata.Has(MetadataKey.LocalTempPath))
        {
            string localPath = item.Metadata.Get<string>(MetadataKey.LocalTempPath)!;
            // Open with FileShare.Read to allow other processes (or ourselves) to read it.
            stream = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
        else
        {
            // Fallback to direct content stream (e.g. if Prep step was skipped)
            stream = item.Content.OpenReadStream();
        }

        try
        {
            // 1. Calculate Hash
            string hash = await ComputeSha256Async(stream, ct);
            item.Metadata.Set(MetadataKey.HashSha256, hash);

            // Reset stream for Metadata reading
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            // 2. Extract Metadata (EXIF/XMP)
            try 
            {
                var directories = ImageMetadataReader.ReadMetadata(stream);

                // Look for EXIF SubIFD
                var subIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
                if (subIfd != null)
                {
                    if (subIfd.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out DateTime dateOriginal))
                    {
                        item.Metadata.Set(MetadataKey.RawExifDate, dateOriginal);
                    }
                }
                
                // Future: Add XMP or other format parsers here
            }
            catch (Exception)
            {
                // Metadata extraction failed (not an image, or corrupt). Non-critical.
            }
        }
        finally
        {
            await stream.DisposeAsync();
        }

        // 3. Date Resolution (Waterfall)
        DateTime finalDate = ResolveAuthoredDate(item);
        item.Metadata.Set(MetadataKey.AuthoredDateTime, finalDate);
    }

    private async Task<string> ComputeSha256Async(Stream stream, CancellationToken ct)
    {
        using var sha256 = SHA256.Create();
        // Unfortunately ComputeHashAsync doesn't support progress reporting easily without custom stream wrapper,
        // but it supports Cancellation in .NET 5+.
        // To ensure responsiveness, we can read in chunks.
        
        byte[] buffer = new byte[81920]; // 80KB
        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
        {
            sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
        }
        sha256.TransformFinalBlock(buffer, 0, 0);
        
        return BitConverter.ToString(sha256.Hash!).Replace("-", "").ToLowerInvariant();
    }

    private DateTime ResolveAuthoredDate(IBackupItem item)
    {
        // Prio 1: Internal Metadata (EXIF)
        if (item.Metadata.Has(MetadataKey.RawExifDate))
        {
            return item.Metadata.Get<DateTime>(MetadataKey.RawExifDate);
        }

        // Prio 2: Device Metadata (Not available in FileSystem scan usually, unless provided)
        if (item.Metadata.Has(MetadataKey.RawDeviceDate))
        {
             return item.Metadata.Get<DateTime>(MetadataKey.RawDeviceDate);
        }

        // Prio 3: File System Creation
        // Accessing FileInfo again via Content is cleaner if specific props needed, 
        // but usually SourceContent abstract shouldn't expose FileInfo directly.
        // But we can assume we might have it or read it.
        // Let's use a fallback logic if we can't get it easily, but usually FileSystemScanner sets it?
        // Actually FileSystemScanner in my impl didn't set dates. Let's read from file now if needed.
        // But wait, the stream is open. We can get FileInfo from path.
        
        try 
        {
            var fi = new FileInfo(item.Content.OriginalPath);
            return fi.CreationTime < fi.LastWriteTime ? fi.CreationTime : fi.LastWriteTime;
        }
        catch
        {
            return DateTime.UtcNow; // Absolute fallback
        }
    }
}
