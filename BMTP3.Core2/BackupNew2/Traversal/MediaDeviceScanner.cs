using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BMTP3.Core2.BackupNew2.Interfaces;
using BMTP3.Core2.BackupNew2.Models;
using BMTP3.Core2.BackupNew2.Models.Configuration;
using BMTP3.Core2.BackupNew2.Models.Internal;
using MediaDevices;

namespace BMTP3.Core2.BackupNew2.Traversal;

[SupportedOSPlatform("windows7.0")]
public sealed class MediaDeviceScanner : IBackupScanner
{
    public async IAsyncEnumerable<IBackupItem> ScanAsync(BackupJob job, [EnumeratorCancellation] CancellationToken ct)
    {
        if (job.SourceType != SourceType.Mtp)
        {
            yield break;
        }

        // 1. Find Device
        var devices = MediaDevice.GetDevices();
        var device = devices.FirstOrDefault(d => d.FriendlyName == job.SourceId || d.Description == job.SourceId);

        if (device == null)
        {
            throw new Exception($"Device not found: {job.SourceId}");
        }

        // 2. Connect
        device.Connect();
        
        try 
        {
             string rootPath = job.SourcePath;
             // MTP paths are like "\Internal Storage\DCIM". Ensure we have a valid root.
             if (!device.DirectoryExists(rootPath))
             {
                 // Try to be lenient or fail? Let's try to list root if path is empty.
                 // If specified path doesn't exist, it's an error for this job.
                 throw new DirectoryNotFoundException($"Path '{rootPath}' not found on device '{job.SourceId}'");
             }
             
             var rootDir = device.GetDirectoryInfo(rootPath);
             
             // Prepare filters
             var includeRegexes = job.IncludePatterns?.Select(GlobToRegex).ToList() ?? new List<Regex>();
             var excludeRegexes = job.ExcludePatterns?.Select(GlobToRegex).ToList() ?? new List<Regex>();

             // 3. Traverse
             await foreach (var mediaFile in ScanInternalAsync(rootDir, job.Recursive, ct))
             {
                 if (!ShouldProcess(mediaFile.Name, includeRegexes, excludeRegexes))
                 {
                     continue;
                 }

                 var content = new MediaDeviceSourceContent(mediaFile);
                 var item = new BackupItem(content);

                 // Metadata
                 // MediaDevice paths are virtual. Construct a relative path manually.
                 // FullName is like "\Internal Storage\DCIM\100APPLE\IMG_001.JPG"
                 // Root is "\Internal Storage\DCIM"
                 // Relative should be "100APPLE\IMG_001.JPG"
                 
                 // Simple relative path hack since Path.GetRelativePath works on file system paths usually
                 string relPath = GetRelativeMtpPath(rootPath, mediaFile.FullName);
                 
                 item.Metadata.Set(MetadataKey.SourceRelativePath, relPath);
                 item.Metadata.Set(MetadataKey.OriginalSourceId, job.SourceId);
                 item.Metadata.Set(MetadataKey.SizeBytes, content.SizeBytes);
                 item.Metadata.Set(MetadataKey.OriginalFileName, content.Name);
                 
                 // MTP usually provides good dates, let's capture them early if possible
                 if (mediaFile.DateAuthored.HasValue)
                     item.Metadata.Set(MetadataKey.RawDeviceDate, mediaFile.DateAuthored.Value);
                 else 
                     item.Metadata.Set(MetadataKey.RawDeviceDate, mediaFile.LastWriteTime ?? DateTime.MinValue);

                 yield return item;
             }

        }
        finally
        {
            device.Disconnect();
            device.Dispose();
        }
    }

    private async IAsyncEnumerable<MediaFileInfo> ScanInternalAsync(MediaDirectoryInfo dir, bool recursive, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach(var file in SafeEnumerateFiles(dir))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return file;
            await Task.Yield();
        }

        if(recursive)
        {
            foreach(var subDir in SafeEnumerateDirectories(dir))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await foreach(var f in ScanInternalAsync(subDir, recursive, cancellationToken))
                {
                    yield return f;
                }
            }
        }
    }

    private static IEnumerable<MediaFileInfo> SafeEnumerateFiles(MediaDirectoryInfo dir)
    {
        try { return dir.EnumerateFiles(); } catch { return Array.Empty<MediaFileInfo>(); }
    }

    private static IEnumerable<MediaDirectoryInfo> SafeEnumerateDirectories(MediaDirectoryInfo dir)
    {
        try { return dir.EnumerateDirectories(); } catch { return Array.Empty<MediaDirectoryInfo>(); }
    }

    private bool ShouldProcess(string filename, List<Regex> includes, List<Regex> excludes)
    {
        if (excludes.Count > 0)
        {
            if (excludes.Any(r => r.IsMatch(filename))) return false;
        }

        if (includes.Count == 0)
        {
            return true;
        }

        return includes.Any(r => r.IsMatch(filename));
    }

    private static Regex GlobToRegex(string glob)
    {
        var regexPattern = "^" + Regex.Escape(glob).Replace("*", ".*").Replace("?", ".") + "$";
        return new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }
    
    private string GetRelativeMtpPath(string root, string full)
    {
        // Normalize separators
        string r = root.Replace('/', '\\').TrimEnd('\\');
        string f = full.Replace('/', '\\');
        
        if (f.StartsWith(r, StringComparison.OrdinalIgnoreCase))
        {
            string sub = f.Substring(r.Length);
            return sub.TrimStart('\\');
        }
        return f;
    }
}
