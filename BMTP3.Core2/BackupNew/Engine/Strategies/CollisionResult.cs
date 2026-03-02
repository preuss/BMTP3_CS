using BMTP3.Core2.BackupNew.Api.Enums;

namespace BMTP3.Core2.BackupNew.Engine.Strategies;
/// <summary>
/// Result of a collision resolution attempt.
/// </summary>
public record CollisionResult(
	BackupActionType Action,
	string TargetPath,
	string Reason
);