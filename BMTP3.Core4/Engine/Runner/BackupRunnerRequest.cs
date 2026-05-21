using BMTP3.Core4.Models;

namespace BMTP3.Core4.Engine.Runner;

internal record BackupRunnerRequest
{
	public IReadOnlyList<BackupRecord> Records { get; init; } = Array.Empty<BackupRecord>();
}
