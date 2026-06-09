using BMTP3.Core4.Api.Models.Enums;

namespace BMTP3.Core4.Api.Models;

public sealed record DriveCatalogEntry
{
	public required string Id { get; init; }

	public required string Name { get; init; }

	public required string RootPath { get; init; }

	public BackupSourceType SourceType { get; init; }

	public long TotalSize { get; init; }

	public long AvailableFreeSpace { get; init; }
}
