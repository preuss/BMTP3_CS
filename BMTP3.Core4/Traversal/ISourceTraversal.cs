namespace BMTP3.Core4.Traversal;

/// <summary>
///     Source-agnostic abstraction for traversing a hierarchical source
///     (filesystem, MTP device, etc.).
/// </summary>
internal interface ISourceTraversal
{
	/// <summary>
	///     Traverses the source according to <paramref name="request" />
	///     and returns discovered items.
	/// </summary>
	IAsyncEnumerable<SourceTraversalItem> TraverseAsync(
		SourceTraversalRequest request,
		IProgress<SourceTraversalProgress>? progress,
		CancellationToken cancellationToken
	);
}