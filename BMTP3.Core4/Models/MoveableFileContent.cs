namespace BMTP3.Core4.Models;

/// <summary>
///     File-backed content that can be moved atomically to a new path.
/// </summary>
/// <remarks>
///     After a successful move, the current instance is invalidated and must no longer be used.
///     A new <see cref="IContent"/> representing the moved file is returned instead.
/// </remarks>
internal class MoveableFileContent : FileContent, IMoveableContent
{
	/// <summary>
	///     Creates a <see cref="MoveableFileContent"/> from a file path.
	/// </summary>
	/// <param name="filePath">Full path to the file.</param>
	public MoveableFileContent(string filePath) : this(new FileInfo(filePath))
	{
	}

	/// <summary>
	///     Creates a <see cref="MoveableFileContent"/> from an existing <see cref="FileInfo"/>.
	/// </summary>
	/// <param name="fileInfo">The file metadata for the underlying file.</param>
	public MoveableFileContent(FileInfo fileInfo) : base(fileInfo)
	{
	}

	/// <summary>
	///     Atomically moves the underlying file to <paramref name="destinationPath"/>.
	/// </summary>
	/// <param name="destinationPath">The destination path for the moved file.</param>
	/// <param name="overwrite">
	///     <c>true</c> to overwrite an existing destination file; otherwise <c>false</c>.
	/// </param>
	/// <returns>
	///     A new <see cref="IContent"/> instance representing the moved file.
	/// </returns>
	/// <remarks>
	///     After a successful move, this instance is invalidated and must no longer be used.
	///     The returned instance becomes the new owner/representation of the file content.
	/// </remarks>
	/// <exception cref="InvalidOperationException">This instance has already been invalidated.</exception>
	public IContent MoveTo(string destinationPath, bool overwrite)
	{
		// Fail fast if this content instance is no longer valid.
		ThrowIfInvalidated();

		// Make sure the destination directory exists.
		string? destDir = Path.GetDirectoryName(destinationPath);
		if(!string.IsNullOrEmpty(destDir))
		{
			Directory.CreateDirectory(destDir);
		}

		// Perform the move.
		// On the same volume this is typically atomic and very fast.
		// Across volumes, the platform may fall back to copy+delete semantics.
		FileInfo.MoveTo(destinationPath, overwrite);

		// This instance no longer represents a valid location after the move.
		Invalidate();

		// Return a fresh content object for the new location.
		return new MoveableFileContent(new FileInfo(destinationPath));
	}
}