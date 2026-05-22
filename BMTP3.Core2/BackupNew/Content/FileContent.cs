namespace BMTP3.Core2.BackupNew.Content;

/// <summary>
///     ISourceContent implementation that reads from a regular file on disk.
/// </summary>
public sealed class FileContent : IContent, IMoveableContent
{
	private bool _disposed;

	public FileContent(string filePath) : this(new FileInfo(filePath))
	{
	}

	public FileContent(FileInfo fileInfo)
	{
		FileInfo = fileInfo ?? throw new ArgumentNullException(nameof(fileInfo));
		if (!FileInfo.Exists)
		{
			throw new FileNotFoundException($"File not found: {fileInfo.FullName}");
		}
	}

	public FileInfo FileInfo { get; }

	public ulong Length => (ulong)FileInfo.Length;

	public Stream OpenRead()
	{
		// return _fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
		// FileShare.ReadWrite allows other processes (e.g. phone sync tools) to keep the file open
		return new FileStream(
			FileInfo.FullName,
			FileMode.Open,
			FileAccess.Read,
			FileShare.Read);
	}

	/// <summary>
	///     Opens a readable stream to the file content asynchronously.
	///     <para>
	///         This implementation returns a <see cref="FileStream" /> wrapped in a Task.
	///         The opening process itself is synchronous, but the returned stream supports asynchronous read operations.
	///     </para>
	/// </summary>
	public Task<Stream> OpenReadStreamAsync(CancellationToken ct)
	{
		// For FileStream, synchronous and asynchronous open are practically the same for now,
		// but we wrap it in a Task to conform to the async interface.
		return Task.FromResult<Stream>(new FileStream(
			FileInfo.FullName,
			FileMode.Open,
			FileAccess.Read,
			FileShare.Read));
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			// Nothing to dispose – the stream is owned by the caller
			_disposed = true;
		}
	}

	/// <summary>
	///     Moves the underlying file to a new location atomically.
	/// </summary>
	public IContent MoveTo(string destinationPath)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);

		// Make sure the destination directory exists
		string? destDir = Path.GetDirectoryName(destinationPath);
		if (!string.IsNullOrEmpty(destDir))
		{
			Directory.CreateDirectory(destDir);
		}

		// Perform atomic move (very fast on same volume)
		// Note: If it's across volumes (e.g. C: to D:), .NET automatically falls back to copy-delete, which is also fine.
		FileInfo.MoveTo(destinationPath, true);

		// Return a new instance pointing to the new path
		// The old instance (this) is now "empty" or invalid, but we return the new truth.
		return new FileContent(destinationPath);
	}
}