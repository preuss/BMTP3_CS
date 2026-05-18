using BMTP3.Core4.Models;

namespace BMTP3.Core4.Traversal;

/// <summary>
///     A single item discovered during source traversal.
///     Contains source path, content access, and available dates.
/// </summary>
internal sealed record SourceTraversalItem
{
	/// <summary>
	///     Unique identifier within the source.
	/// </summary>
	public required string Id { get; init; }

	/// <summary>
	///     Full path of the item as it exists in the source.
	/// </summary>
	public required string SourcePath { get; init; }

	/// <summary>
	///     Provides access to the item's byte content.
	/// </summary>
	public required IContent Content { get; init; }

	/// <summary>
	///     Source creation date, if available.
	/// </summary>
	public DateTimeOffset? DateCreated { get; init; }

	/// <summary>
	///     Source last write / modified date, if available.
	/// </summary>
	public DateTimeOffset? DateModified { get; init; }

	/// <summary>
	///     Source authored date, if available (common on MTP media files).
	/// </summary>
	public DateTimeOffset? DateAuthored { get; init; }

	/// <summary>
	///     Source last access date, if available.
	/// </summary>
	public DateTimeOffset? DateAccessed { get; init; }
}
