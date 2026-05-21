using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Models;

namespace BMTP3.Core4.Engine.Runner;

	internal record BackupRunnerRequest
{
	public IReadOnlyList<BackupRecord> Records { get; init; } = Array.Empty<BackupRecord>();

	public string Destination { get; init; } = string.Empty;
	public OutputStructureStrategy OutputStructureStrategy { get; init; }
	public CollisionStrategy CollisionStrategy { get; init; }
	public SidecarFormat SidecarFormat { get; init; }
	public bool StopOnError { get; init; }

	public DateTimeOffset BackupStartTime { get; init; }
}
