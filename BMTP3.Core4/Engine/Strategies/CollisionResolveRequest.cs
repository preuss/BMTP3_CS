using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Hashing;
using BMTP3.Core4.Models;

namespace BMTP3.Core4.Engine.Strategies;

internal sealed record CollisionResolveRequest
{
	public required string SourcePath { get; init; }
	public required string IntendedTargetPath { get; init; }

	public required string RelativeFilePath { get; init; }
	public required DateTimeOffset CreateFileDate { get; init; }
	public required string ItemId { get; init; }

	public string? StrongHash { get; init; }
	public IReadOnlyDictionary<HashType, string>? ComputedHashes { get; init; }
	public BackupSourceDetails? SourceDetails { get; init; }

	public required CollisionStrategy Strategy { get; init; }

	public required CollisionComparisonType ComparisonType { get; init; }

	public required RenameStrategy RenameStrategy { get; init; }
	public string? CustomRenamePattern { get; init; }

	public required IReadOnlyList<HashAlgorithmType> ComparisonHashAlgorithmTypes { get; init; }
}