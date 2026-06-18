using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Hashing;
using BMTP3.Core4.Models;

namespace BMTP3.Core4.Engine.Strategies;

internal sealed record RenameCollisionRequest(
	string SourcePath,
	string IntendedTargetPath,
	string RelativeFilePath,
	string FileName,
	DateTimeOffset CreateFileDate,
	string? StrongHash,
	string ItemId,
	RenameStrategy RenameStrategy,
	string? CustomRenamePattern,
	CollisionComparisonType ComparisonType,
	IReadOnlyList<HashAlgorithmType> ComparisonHashAlgorithmTypes,
	IReadOnlyDictionary<HashType, string>? ComputedHashes,
	BackupSourceDetails? SourceDetails
);