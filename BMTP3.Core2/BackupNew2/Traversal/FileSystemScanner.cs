using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BMTP3.Core2.BackupNew2.Interfaces;
using BMTP3.Core2.BackupNew2.Models;
using BMTP3.Core2.BackupNew2.Models.Configuration;
using BMTP3.Core2.BackupNew2.Models.Internal;

namespace BMTP3.Core2.BackupNew2.Traversal;

/// <summary>
/// Traverses a local file system and yields IBackupItems.
/// Combines robust traversal logic with the new IBackupScanner interface.
/// </summary>
public sealed class FileSystemScanner : IBackupScanner
{
    public async IAsyncEnumerable<IBackupItem> ScanAsync(BackupJob job, [EnumeratorCancellation] CancellationToken ct)
    {
        if (job.SourceType != SourceType.FileSystem)
        {
            yield break;
        }

        string rootPath = job.SourcePath;
        if (!Directory.Exists(rootPath))
        {
             yield break;
        }

        // Prepare filters
        var includeRegexes = job.IncludePatterns?.Select(GlobToRegex).ToList() ?? new List<Regex>();
        var excludeRegexes = job.ExcludePatterns?.Select(GlobToRegex).ToList() ?? new List<Regex>();

        DirectoryInfo dirInfo = new(rootPath);

        await foreach (var fileInfo in ScanInternalAsync(dirInfo, job.Recursive, ct))
        {
            if (!ShouldProcess(fileInfo.FullName, includeRegexes, excludeRegexes))
            {
                continue;
            }

            var content = new FileSourceContent(fileInfo);
            var item = new BackupItem(content);
            
            // Basic metadata
            item.Metadata.Set(MetadataKey.SourceRelativePath, Path.GetRelativePath(rootPath, fileInfo.FullName));
            item.Metadata.Set(MetadataKey.OriginalSourceId, job.SourceId);
            item.Metadata.Set(MetadataKey.SizeBytes, content.SizeBytes);
            item.Metadata.Set(MetadataKey.OriginalFileName, content.Name);

            yield return item;
        }
    }

    private async IAsyncEnumerable<FileInfo> ScanInternalAsync(DirectoryInfo dir, bool recursive, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Process files in current dir
        foreach (var file in SafeGetFiles(dir))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return file;
            
            // Cooperative yield
            await Task.Yield();
        }

        // Process subdirectories
        if (recursive)
        {
            foreach (var subDir in SafeGetDirectories(dir))
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                await foreach (var f in ScanInternalAsync(subDir, recursive, cancellationToken))
                {
                    yield return f;
                }
            }
        }
    }

    private static IEnumerable<FileInfo> SafeGetFiles(DirectoryInfo dir)
    {
        try { return dir.GetFiles(); } catch { return Array.Empty<FileInfo>(); }
    }

    private static IEnumerable<DirectoryInfo> SafeGetDirectories(DirectoryInfo dir)
    {
        try { return dir.GetDirectories(); } catch { return Array.Empty<DirectoryInfo>(); }
    }

    private bool ShouldProcess(string fullPath, List<Regex> includes, List<Regex> excludes)
    {
        string filename = Path.GetFileName(fullPath);

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
        var regexPattern = "^" + Regex.Escape(glob).Replace("\\*", ".*").Replace("\\?", ".") + "$";
        return new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }
}