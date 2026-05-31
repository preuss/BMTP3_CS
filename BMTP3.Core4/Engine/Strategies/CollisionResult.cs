namespace BMTP3.Core4.Engine.Strategies;

internal sealed record CollisionResult(
	CollisionResolutionAction Action,
	string TargetPath
);
