using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Hashing;

namespace BMTP3.Core4.Engine.Sidecar;

internal record SidecarRequest
{
	public required SidecarFormat Format { get; init; }

	// ---------- Source ----------
	public required string SourceType { get; init; }
	public required string SourceFileName { get; init; }
	public string? SourcePersistentUniqueId { get; init; }
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
	public required string SourceRelativePath { get; init; }
	public string? SanitizedSourceRelativePath { get; init; }
	public required string TargetRelativePath { get; init; }

	// ---------- Hashes ----------
	public IReadOnlyDictionary<HashType, string>? Hashes { get; init; }

	// ---------- Source details (optional) ----------
	// Section name: "SourceDevice" or "SourceDrive"
	public string? SourceDetailsSectionName { get; init; }
	public IReadOnlyDictionary<string, string>? SourceDetails { get; init; }
}
