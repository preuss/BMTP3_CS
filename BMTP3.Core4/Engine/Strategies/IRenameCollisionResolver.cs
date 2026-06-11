using BMTP3.Core4.Infrastructure.Throttling;

namespace BMTP3.Core4.Engine.Strategies;

internal interface IRenameCollisionResolver
{
	Task<RenameCollisionResult> ResolveAsync(RenameCollisionRequest request, IThrottler throttler, CancellationToken ct);
}
