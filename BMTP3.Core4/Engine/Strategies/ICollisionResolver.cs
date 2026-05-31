namespace BMTP3.Core4.Engine.Strategies;

internal interface ICollisionResolver
{
	Task<CollisionResult> ResolveAsync(CollisionResolveRequest request, CancellationToken ct);
}
