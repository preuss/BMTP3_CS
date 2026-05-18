namespace BMTP3.Core4.Traversal;

/// <summary>
/// Snapshot of traversal progress.
/// </summary>
internal sealed record SourceTraversalProgress
{
	/// <summary>
	/// Number of directories traversed so far.
	/// </summary>
	public int DirectoriesTraversed { get; init; }

	/// <summary>
	/// Number of files discovered so far.
	/// </summary>
	public int FilesDiscovered { get; init; }
}
