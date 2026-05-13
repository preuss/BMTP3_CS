using BMTP3.Core4.Models.Enums;

namespace BMTP3.Core4.Models;

/// <summary>
/// Result of a single item (file, directory, etc.) processed during a backup job.
/// </summary>
public sealed record BackupJobResultItem
{
    public string Id { get; init; } = string.Empty;
    public string SourcePath { get; init; } = string.Empty;
    public string? DestinationPath { get; init; }
    public long Size { get; init; }
    public BackupJobItemState State { get; init; }
}
