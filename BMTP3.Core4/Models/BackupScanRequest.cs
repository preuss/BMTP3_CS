namespace BMTP3.Core4.Models;

internal sealed record BackupScanRequest(
	string SourcePath,
	bool Recursive,
	IReadOnlyList<string>? IncludePatterns,
	IReadOnlyList<string>? ExcludePatterns
);
