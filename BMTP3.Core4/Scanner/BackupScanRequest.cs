using BMTP3.Core4.Api.Models.Enums;

namespace BMTP3.Core4.Scanner;

/// <summary>
/// Parameters for controlling the behavior of the backup scanning process.
/// </summary>
internal sealed record BackupScanRequest
{
	/// <summary>
	/// The root path within the source to start scanning from.
	/// </summary>
	public required string SourcePath { get; init; }

	/// <summary>
	/// Whether to recurse into subdirectories.
	/// </summary>
	public bool Recursive { get; init; } = true;

	/// <summary>
	/// Glob patterns to include. Empty or null = include all.
	/// </summary>
	public IReadOnlyList<string>? IncludePatterns { get; init; }

	/// <summary>
	/// Glob patterns to exclude. Empty or null = exclude none.
	/// Exclusion takes precedence over inclusion.
	/// </summary>
	public IReadOnlyList<string>? ExcludePatterns { get; init; }

	/// <summary>
	/// Controls how items are identified during scanning.
	/// </summary>
	public ItemIdScope ItemIdScope { get; init; } = ItemIdScope.Connection;
}
