namespace BMTP3.Core2.BackupNew.Content;
/// <summary>
/// ISourceContent implementation that reads from a regular file on disk.
/// </summary>
public sealed class FileContent : IContent, IMoveableContent
{
	private readonly FileInfo _fileInfo;
	private bool _disposed;

	public FileContent(string filePath) : this(new FileInfo(filePath))
	{
	}
	public FileContent(FileInfo fileInfo)
	{
		_fileInfo = fileInfo ?? throw new ArgumentNullException(nameof(fileInfo));
		if(!_fileInfo.Exists)
		{
			throw new FileNotFoundException($"File not found: {fileInfo.FullName}");
		}
	}

	public FileInfo FileInfo => _fileInfo;
	public ulong Length => (ulong)_fileInfo.Length;

	public Stream OpenRead()
	{
		// return _fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
		// FileShare.ReadWrite allows other processes (e.g. phone sync tools) to keep the file open
		return new FileStream(
			_fileInfo.FullName,
			FileMode.Open,
			FileAccess.Read,
			FileShare.Read);
	}

	public void Dispose()
	{
		if(!_disposed)
		{
			// Nothing to dispose – the stream is owned by the caller
			_disposed = true;
		}
	}

	/// <summary>
	/// Moves the underlying file to a new location atomically.
	/// </summary>
	public IContent MoveTo(string destinationPath)
	{
		if(_disposed) throw new ObjectDisposedException(nameof(FileContent));

		// Make sure the destination directory exists
		var destDir = Path.GetDirectoryName(destinationPath);
		if(!string.IsNullOrEmpty(destDir))
		{
			Directory.CreateDirectory(destDir);
		}

		// Perform atomic move (very fast on same volume)
		// Note: If it's across volumes (e.g. C: to D:), .NET automatically falls back to copy-delete, which is also fine.
		_fileInfo.MoveTo(destinationPath, overwrite: true);

		// Return a new instance pointing to the new path
		// The old instance (this) is now "empty" or invalid, but we return the new truth.
		return new FileContent(destinationPath);
	}
}