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
	// 0 = valid, 1 = invalidated
	private int _invalidated;

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
	/// <exception cref="InvalidOperationException">
	///     This instance has already been invalidated.
	/// </exception>
	public IContent MoveTo(string destinationPath, bool overwrite)
	{
		// Atomically invalidate this instance.
		// If another thread already moved it, we fail fast.
		if (Interlocked.Exchange(ref _invalidated, 1) != 0)
		{
			throw new InvalidOperationException("This instance has already been invalidated.");
		}

		// Ensure the destination directory exists.
		string? destDir = Path.GetDirectoryName(destinationPath);
		if (!string.IsNullOrEmpty(destDir))
		{
			Directory.CreateDirectory(destDir);
		}

		// Perform the move.
		// - Same volume: typically atomic and very fast (rename)
		// - Different volume: may fall back to copy + delete
		FileInfo.MoveTo(destinationPath, overwrite);

		// Return a fresh content instance representing the new location.
		return new MoveableFileContent(new FileInfo(destinationPath));
	}
}