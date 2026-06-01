namespace BMTP3.Core4.Models;

internal class MoveableFileContent : FileContent, IMoveableContent
{
	public MoveableFileContent(string filePath) : this(new FileInfo(filePath))
	{
	}

	public MoveableFileContent(FileInfo fileInfo) : base(fileInfo)
	{
	}

	public IContent MoveTo(string destinationPath, bool overwrite)
	{
		// Fail-fast if already disposed/invalid
		ThrowIfDisposed();

		// Make sure the destination directory exists
		string? destDir = Path.GetDirectoryName(destinationPath);
		if (!string.IsNullOrEmpty(destDir))
		{
			Directory.CreateDirectory(destDir);
		}

		// Perform atomic move (very fast on same volume)
		// Note: If it's across volumes (e.g. C: to D:), .NET automatically falls back to copy-delete, which is also fine.
		FileInfo.MoveTo(destinationPath, overwrite);

		// Mark this instance as disposed/invalid after the move.
		MarkDisposed();

		// Return a new MoveableFileContent that represents the moved file.
		return new MoveableFileContent(new FileInfo(destinationPath));
	}
}
