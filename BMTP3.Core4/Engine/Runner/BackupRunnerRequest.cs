using BMTP3.Core4.Api.Models.Enums;

namespace BMTP3.Core4.Engine.Runner;

internal record BackupRunnerRequest
{
	public SidecarFormat SidecarFormat { get; init; }
	public CollisionStrategy CollisionStrategy { get; init; }
	public DateTimeOffset BackupStartTime { get; init; }
}
