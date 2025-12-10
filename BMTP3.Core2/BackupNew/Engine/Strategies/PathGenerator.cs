using System.Text.RegularExpressions;
using BMTP3.Core2.BackupNew.Api;
using BMTP3.Core2.BackupNew.Api.Enums;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Domain.Item;

namespace BMTP3.Core2.BackupNew.Engine.Strategies;

/// <summary>
/// Standard implementation of IPathGenerator.
/// Handles Flat, PreserveSourceTree and CustomPathPattern strategies.
/// </summary>
public class PathGenerator : IPathGenerator
{
    private static readonly Regex TokenRegex = new(@"\$\{?([a-zA-Z0-9_]+)\}?", RegexOptions.Compiled);

    public string GenerateRelativePath(IBackupItem item, BackupPlan plan)
    {
        string fileName = item.Metadata.Get<string>(MetadataKey.SourceFileName) 
                          ?? throw new InvalidOperationException("SourceFileName is missing in metadata.");

        switch (plan.OutputStrategy)
        {
            case OutputStructureStrategy.Flat:
                return fileName;

            case OutputStructureStrategy.PreserveSourceTree:
                var relPath = item.Metadata.Get<string>(MetadataKey.SourceRelativePath);
                
                if (string.IsNullOrWhiteSpace(relPath))
                {
                    return fileName;
                }
                
                return Path.Combine(relPath, fileName);

            case OutputStructureStrategy.CustomPathPattern:
                if (string.IsNullOrWhiteSpace(plan.CustomOutputPathPattern))
                {
                    return fileName;
                }
                return FormatPattern(plan.CustomOutputPathPattern, item, fileName);

            default:
                return fileName;
        }
    }

    private string FormatPattern(string pattern, IBackupItem item, string originalFileName)
    {
        return TokenRegex.Replace(pattern, match =>
        {
            string key = match.Groups[1].Value;
            return GetTokenValue(key, item, originalFileName);
        });
    }

    private string GetTokenValue(string key, IBackupItem item, string originalFileName)
    {
        DateTime date = GetBestDate(item);

        switch (key)
        {
            case "yyyy": return date.ToString("yyyy");
            case "MM": return date.ToString("MM");
            case "dd": return date.ToString("dd");
            case "HH": return date.ToString("HH");
            case "mm": return date.ToString("mm");
            case "ss": return date.ToString("ss");
            
            case "originalName": 
            case "filename":
                return Path.GetFileNameWithoutExtension(originalFileName);
            case "ext":
            case "extension":
                return Path.GetExtension(originalFileName).TrimStart('.');
            case "originalFullName":
                return originalFileName;

            case "relativePath":
            case "sourceRelativePath":
                return item.Metadata.Get<string>(MetadataKey.SourceRelativePath) ?? "";

            case "deviceName":
            case "sourceId":
                return item.Metadata.Get<string>(MetadataKey.SourceId) ?? "unknown_source";

            default:
                return "";
        }
    }

    private DateTime GetBestDate(IBackupItem item)
    {
        if (item.Metadata.Has(MetadataKey.AuthoredDateTime))
            return item.Metadata.Get<DateTime>(MetadataKey.AuthoredDateTime);

        if (item.Metadata.Has(MetadataKey.CreatedDateTime))
            return item.Metadata.Get<DateTime>(MetadataKey.CreatedDateTime);

        if (item.Metadata.Has(MetadataKey.ModifiedDateTime))
            return item.Metadata.Get<DateTime>(MetadataKey.ModifiedDateTime);

        return DateTime.Now;
    }
}
