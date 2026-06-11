using BMTP3.Core4.Infrastructure.Throttling;

namespace BMTP3.Core4.Engine.Strategies;

internal interface ICollisionResolver
{
	Task<CollisionResult> ResolveAsync(CollisionResolveRequest request, IThrottler throttler, CancellationToken ct);
}
