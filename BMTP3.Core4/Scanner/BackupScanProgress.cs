using BMTP3.Core4.Api.Models.Enums;

namespace BMTP3.Core4.Scanner;

internal sealed record BackupScanProgress
{
	public string SourcePath { get; init; } = string.Empty;
	public BackupProgressPhase CurrentPhase { get; init; }
	public int DirectoriesTraversed { get; init; }
	public int FilesDiscovered { get; init; }
}
