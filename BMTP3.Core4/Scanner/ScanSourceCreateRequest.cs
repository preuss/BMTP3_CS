using BMTP3.Core4.Api.Models.Enums;

namespace BMTP3.Core4.Scanner;

internal sealed record ScanSourceCreateRequest
{
	public required BackupSourceType SourceType { get; init; }

	public required string SourcePath { get; init; }
}
