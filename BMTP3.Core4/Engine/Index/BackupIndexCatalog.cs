namespace BMTP3.Core4.Engine.Index;

internal sealed record BackupIndexCatalog
{
	public required string BackupName { get; init; }
	public required DateTimeOffset CreatedAt { get; init; }
	public required string SessionId { get; init; }
	public required string SourcePath { get; init; }
	public required string Destination { get; init; }
	public required int TotalFiles { get; init; }
	public required long TotalBytes { get; init; }
	public required int CompletedFiles { get; init; }
	public required string State { get; init; }
	public required IReadOnlyList<BackupIndexFileEntry> Files { get; init; }
}

internal sealed record BackupIndexFileEntry
{
	public required string Id { get; init; }
	public required string SourcePath { get; init; }
	public required string RelativePath { get; init; }
	public required string FileName { get; init; }
	public string? DestinationPath { get; init; }
	public required long Length { get; init; }
	public required string Status { get; init; }
	public Dictionary<string, string>? Hashes { get; init; }
	public BackupIndexTimestamps? Timestamps { get; init; }
}

internal sealed record BackupIndexTimestamps
{
	public DateTimeOffset? MediaTaken { get; init; }
	public DateTimeOffset? Created { get; init; }
	public DateTimeOffset? Modified { get; init; }
	public DateTimeOffset? Authored { get; init; }
	public DateTimeOffset? Accessed { get; init; }
}
