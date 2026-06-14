using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Hashing;
using BMTP3.Core4.Models;

namespace BMTP3.Core4.Engine.Sidecar;

internal record SidecarRequest
{
	public required SidecarFormat Format { get; init; }

	// ---------- Source ----------
	public required BackupSourceType SourceType { get; init; }
	public required string SourceFileName { get; init; }
	public string? SourceId { get; init; }
	public string? SourceFullPath { get; init; }

	// Date fields — [Source] section
	public DateTimeOffset? MediaTakenDateTime { get; init; }
	public DateTimeOffset? AuthoredDateTime { get; init; }
	public DateTimeOffset? CreateDateTime { get; init; }
	public DateTimeOffset? LastWriteDateTime { get; init; }
	public DateTimeOffset? LastAccessDateTime { get; init; }

	// ---------- Backup ----------
	public required DateTimeOffset BackupStartDateTime { get; init; }

	// ---------- Path ----------
	public required string SourceRelativeFilePath { get; init; }
	public string? SanitizedSourceRelativeFilePath { get; init; }
	public required string TargetRelativeFilePath { get; init; }

	// ---------- Hashes ----------
	public IReadOnlyDictionary<HashType, string>? Hashes { get; init; }

	// ---------- Source details ----------
	public required BackupSourceDetails SourceDetails { get; init; }
}
