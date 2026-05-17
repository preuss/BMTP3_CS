namespace BMTP3.Core4.Models;

internal interface IMoveableContent : IContent
{
	/// <summary>
	///     Moves the physical file to a new location and returns a new ISourceContent representing that location.
	///     This is an atomic operation on the file system.
	/// </summary>
	IContent MoveTo(string destinationPath);
}
