using BMTP3.Core4.Api.Models.Enums;

namespace BMTP3.Core4.Engine.Strategies;

internal sealed class CollisionResolver : ICollisionResolver
{
	private readonly IRenameCollisionResolver _renameCollisionResolver;

	public CollisionResolver(IRenameCollisionResolver renameCollisionResolver)
	{
		_renameCollisionResolver = renameCollisionResolver ?? throw new ArgumentNullException(nameof(renameCollisionResolver));
	}

	public async Task<CollisionResult> ResolveAsync(CollisionResolveRequest request, CancellationToken ct)
	{
		return request.Strategy switch
		{
			CollisionStrategy.Overwrite => new(CollisionResolutionAction.Overwrite, request.IntendedTargetPath),
			CollisionStrategy.Skip => new(CollisionResolutionAction.Skip, TargetPath: request.IntendedTargetPath),
			CollisionStrategy.Error => throw new IOException($"Destination already exists: {request.IntendedTargetPath}"),
			CollisionStrategy.Rename => await HandleRenameAsync(CreateRenameCollisionRequest(request), ct),
			_ => throw new ArgumentOutOfRangeException(nameof(request.Strategy), request.Strategy, null),
		};
	}

	private static RenameCollisionRequest CreateRenameCollisionRequest(CollisionResolveRequest request)
	{
		return new RenameCollisionRequest(
			SourcePath: request.SourcePath,
			IntendedTargetPath: request.IntendedTargetPath,
			RelativeFilePath: request.RelativeFilePath,
			FileName: Path.GetFileName(request.IntendedTargetPath),
			CreateFileDate: request.CreateFileDate,
			StrongHash: request.StrongHash,
			ItemId: request.ItemId,
			RenameStrategy: request.RenameStrategy,
			CustomRenamePattern: request.CustomRenamePattern,
			ComparisonType: request.ComparisonType,
			ComparisonHashAlgorithmTypes: request.ComparisonHashAlgorithmTypes,
			ComputedHashes: request.ComputedHashes,
			DeviceName: request.DeviceName,
			DeviceModel: request.DeviceModel
		);
	}

	private async Task<CollisionResult> HandleRenameAsync(RenameCollisionRequest request, CancellationToken ct)
	{
		RenameCollisionResult renameCollisionResult = await _renameCollisionResolver.ResolveAsync(request, ct);


		if(string.IsNullOrWhiteSpace(renameCollisionResult.TargetPath))
		{
			throw new InvalidOperationException($"Rename collision resolver returned action '{renameCollisionResult.Action}' without a target path.");
		}

		return renameCollisionResult.Action switch
		{
			CollisionResolutionAction.Move => new(CollisionResolutionAction.Move, renameCollisionResult.TargetPath),
			CollisionResolutionAction.Skip => new CollisionResult(CollisionResolutionAction.Skip, renameCollisionResult.TargetPath),
			_ => throw new ArgumentOutOfRangeException(nameof(renameCollisionResult.Action), renameCollisionResult.Action, "Unexpected rename collision resolution action."),
		};
	}
}
