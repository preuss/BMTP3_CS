namespace BMTP3.Core4.Engine.Strategies;

internal sealed record RenameCollisionResult(
	CollisionResolutionAction Action,
	string TargetPath
);
