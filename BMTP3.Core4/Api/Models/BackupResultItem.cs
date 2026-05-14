using BMTP3.Core4.Api.Models.Enums;

namespace BMTP3.Core4.Api.Models;

/// <summary>
/// Result of a single item (file, directory, etc.) processed during a backup job.
/// </summary>
public sealed record BackupResultItem
{
    public string Id { get; init; } = string.Empty;
    public string SourcePath { get; init; } = string.Empty;
    public string? DestinationPath { get; init; }
    public long Length { get; init; }
    public BackupResultItemState State { get; init; }
}
