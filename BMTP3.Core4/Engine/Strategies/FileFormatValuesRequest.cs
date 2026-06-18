using BMTP3.Core4.Models;

namespace BMTP3.Core4.Engine.Strategies;

internal sealed record FileFormatValuesRequest(
	string FileName,
	string RelativeFilePath,
	DateTimeOffset CreateFileDate,
	string ItemId,
	string? StrongHash,
	BackupSourceDetails? SourceDetails
);