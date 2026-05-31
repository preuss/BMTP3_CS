using BMTP3.Core4.Api.Models.Enums;

namespace BMTP3.Core4.Engine.Strategies;

internal sealed record RenameCollisionRequest(
	string SourcePath,
	string IntendedTargetPath,
	string RelativePath,
	string FileName,
	DateTimeOffset CreateFileDate,
	string? StrongHash,
	string ItemId,
	RenameStrategy RenameStrategy,
	string? CustomRenamePattern,
	CollisionComparisonType ComparisonType,
	IReadOnlyList<HashAlgorithmType> ComparisonHashAlgorithmTypes,
	string? DeviceName,
	string? DeviceModel
);