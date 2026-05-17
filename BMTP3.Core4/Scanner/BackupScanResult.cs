using BMTP3.Core4.Models;

namespace BMTP3.Core4.Scanner;

internal sealed record BackupScanResult(
	BackupItem Item,
	int DirectoriesTraversed
);
