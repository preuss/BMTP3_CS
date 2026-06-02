using BMTP3.Core4.Engine.Strategies;

namespace BMTP3.Core4.Tests.Fakes;

internal sealed class FakeRenameCollisionResolver : IRenameCollisionResolver
{
	private readonly Func<RenameCollisionRequest, CancellationToken, Task<RenameCollisionResult>> _handler;

	public FakeRenameCollisionResolver() : this((_, _) =>
		Task.FromResult(new RenameCollisionResult(CollisionResolutionAction.Move, "target_1.txt")))
	{
	}

	public FakeRenameCollisionResolver(Func<RenameCollisionRequest, CancellationToken, Task<RenameCollisionResult>> handler)
	{
		_handler = handler;
	}

	public Task<RenameCollisionResult> ResolveAsync(RenameCollisionRequest request, CancellationToken ct) =>
		_handler(request, ct);
}
