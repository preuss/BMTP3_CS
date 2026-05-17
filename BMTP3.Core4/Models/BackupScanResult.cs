namespace BMTP3.Core4.Models;

internal sealed record BackupScanResult(
	BackupItem Item, 
	int DirectoriesTraversed
);
