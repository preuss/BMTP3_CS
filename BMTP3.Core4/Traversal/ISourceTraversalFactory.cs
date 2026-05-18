namespace BMTP3.Core4.Traversal;

/// <summary>
///     Factory for creating <see cref="ISourceTraversal" /> instances.
/// </summary>
internal interface ISourceTraversalFactory
{
	/// <summary>
	///     Creates an <see cref="ISourceTraversal" /> for the given source.
	///     The caller must dispose the returned instance.
	/// </summary>
	ISourceTraversal Create(SourceTraversalFactoryCreateRequest request);
}
