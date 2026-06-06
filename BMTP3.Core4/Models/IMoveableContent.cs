namespace BMTP3.Core4.Models;

internal interface IMoveableContent : IContent, IFileInfoSource
{
	/// <summary>
	///     Atomically moves the physical file to <paramref name="destinationPath"/>.
	///     The current instance is invalidated and must no longer be used.
	///     Returns a new <see cref="IContent"/> pointing to the moved file.
	/// </summary>
	IContent MoveTo(string destinationPath, bool overwrite);
}