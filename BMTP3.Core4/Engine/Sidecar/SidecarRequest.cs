using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Hashing;

namespace BMTP3.Core4.Engine.Sidecar;

internal record SidecarRequest
{
	public required SidecarFormat Format { get; init; }
	public required string OriginalFileName { get; init; }
	public required string RelativePath { get; init; }
	public DateTimeOffset? CreateDateTime { get; init; }
	public DateTimeOffset? AccessDateTime { get; init; }
	public DateTimeOffset? ModifyDateTime { get; init; }
	public DateTimeOffset? AuthoredDateTime { get; init; }
	public IReadOnlyDictionary<HashType, string>? Hashes { get; init; }
	public required DateTimeOffset BackupStartTime { get; init; }
}
