using BMTP3.Core4.Storage;

namespace BMTP3.Core4.Traversal;

/// <summary>
///     Factory for creating <see cref="ISourceTraversal" /> instances
///     from a connected source and its matching drive info.
/// </summary>
internal interface ISourceTraversalFactory
{
	/// <summary>
	///     Creates an <see cref="ISourceTraversal" /> for the given connected source.
	///     <paramref name="connectedSource" /> is used to determine the traversal strategy
	///     (file-system vs. media device).
	///     The caller must dispose the returned instance.
	/// </summary>
	/// <param name="connectedSource">The connected source to traverse.</param>
	/// <returns>An <see cref="ISourceTraversal" /> instance configured for the source type.</returns>
	ISourceTraversal Create(IConnectedSource connectedSource);
}
