namespace BMTP3.Core4.State;

public sealed record BackupSummaryItem
{
	public required string Id { get; init; }
	public required string SourcePath { get; init; }
	public required string RelativePath { get; init; }
	public required string FileName { get; init; }
	public long Length { get; init; }
	public DateTimeOffset? LastModified { get; init; }
	public DateTimeOffset? DateCreated { get; init; }
	public DateTimeOffset? DateAuthored { get; init; }
	public string? DestinationPath { get; init; }
	public bool IsCompleted { get; init; }
	public DateTimeOffset? CompletedAt { get; init; }
	public string? ErrorMessage { get; init; }
}
