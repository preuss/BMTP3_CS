namespace BMTP3.Core4.Scanner;

internal sealed record BackupScanRequest
{
	public bool Recursive { get; init; } = true;

	public IReadOnlyList<string>? IncludePatterns { get; init; }

	public IReadOnlyList<string>? ExcludePatterns { get; init; }
}
