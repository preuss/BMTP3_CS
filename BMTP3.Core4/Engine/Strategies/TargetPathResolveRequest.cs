using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Models;

namespace BMTP3.Core4.Engine.Strategies;

internal sealed record TargetPathResolveRequest(
	string DestinationRoot,
	string RelativeDirectoryPath,
	string FileName,
	DateTimeOffset CreateFileDate,
	string? StrongHash,
	string ItemId,
	OutputStructureStrategy OutputStructureStrategy,
	string? CustomPattern,
	BackupSourceDetails? SourceDetails
);
