namespace BMTP3.Core4.Engine.Strategies;

internal interface IRenameCollisionResolver
{
	Task<RenameCollisionResult> ResolveAsync(RenameCollisionRequest request, CancellationToken ct);
}
