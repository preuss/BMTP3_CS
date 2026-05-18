using BMTP3.Core4.Api.Models.Enums;

namespace BMTP3.Core4.Traversal;

/// <summary>
///     Describes which source to open for traversal.
/// </summary>
internal sealed record SourceTraversalFactoryCreateRequest
{
	/// <summary>
	///     The type of source (FileSystem, MediaDevice, etc.).
	/// </summary>
	public required BackupSourceType SourceType { get; init; }

	/// <summary>
	///     The root path within the source to start traversal from.
	/// </summary>
	public required string SourcePath { get; init; }
}
