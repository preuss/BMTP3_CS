namespace BMTP3.Core4.Models;
internal class FileContent : IContent, IFileInfoSource
{
	private bool _disposed;
	private const int bufferSize = 128 * 1024; // Default is 4096, this is 128 kb - larger buffer size can improve performance for large files.

	/// <summary>
	///		Creates a <see cref="FileContent"/> that represents a regular file on disk.
	/// </summary>
	/// <param name="filePath">Full path to the file.</param>
	/// <exception cref="ArgumentNullException"><paramref name="filePath"/> is <c>null</c>.</exception>
	/// <exception cref="FileNotFoundException">The file does not exist.</exception>
	public FileContent(string filePath) : this(new FileInfo(filePath))
	{
	}

	/// <summary>
	///		Creates a <see cref="FileContent"/> from an existing <see cref="FileInfo"/>.
	/// </summary>
	/// <param name="fileInfo">The <see cref="FileInfo"/> describing the file. Must exist.</param>
	/// <exception cref="ArgumentNullException"><paramref name="fileInfo"/> is <c>null</c>.</exception>
	/// <exception cref="FileNotFoundException">The file does not exist.</exception>
	public FileContent(FileInfo fileInfo)
	{
		ArgumentNullException.ThrowIfNull(fileInfo);
		if(!fileInfo.Exists)
		{
			throw new FileNotFoundException($"File not found: {fileInfo.FullName}", fileInfo.FullName);
		}
		FileInfo = fileInfo;
	}

	/// <summary>
	///		The underlying <see cref="FileInfo"/> for this content.
	///		This property is protected so that only derived types can access the file metadata and perform operations that require the <see cref="FileInfo"/>.
	/// </summary>
	protected FileInfo FileInfo { get; }

	/// <summary>
	///		Length of the underlying file in bytes.
	/// </summary>
	/// <remarks>
	///		Throws <see cref="ObjectDisposedException"/> if this instance has been disposed.
	/// </remarks>
	public ulong Length
	{
		get
		{
			ThrowIfDisposed();
			return (ulong)FileInfo.Length;
		}
	}

	public bool TryGetFileInfo(out FileInfo fileInfo)
	{
		fileInfo = FileInfo;
		return true;
	}

	/// <summary>
	///		Marks this instance as disposed. There is no managed resource to free; the returned streams are owned and disposed by the caller.
	/// </summary>
	public void Dispose()
	{
		MarkDisposed();
	}

	/// <summary>
	///		Asynchronous dispose. Equivalent to <see cref="Dispose"/> for this implementation.
	/// </summary>
	public ValueTask DisposeAsync()
	{
		Dispose();
		return ValueTask.CompletedTask;
	}

	/// <summary>
	///		Open a synchronous <see cref="Stream"/> for reading.
	/// </summary>
	/// <returns>An open <see cref="FileStream"/> for reading. Caller must dispose the stream.</returns>
	/// <exception cref="ObjectDisposedException">If this instance has been disposed.</exception>
	public Stream OpenRead()
	{
		ThrowIfDisposed();

		return new FileStream(
			FileInfo.FullName,
			FileMode.Open,
			FileAccess.Read,
			FileShare.Read,
			bufferSize
		);
	}

	/// <summary>
	///		Open a <see cref="Stream"/> configured for asynchronous read operations.
	/// </summary>
	/// <param name="ct">Cancellation token (currently not used for opening the stream, provided for API symmetry).</param>
	/// <returns>
	///		A <see cref="Task{Stream}"/> that returns an open <see cref="FileStream"/> configured with
	///		<see cref="FileOptions.Asynchronous"/> and <see cref="FileOptions.SequentialScan"/>.
	///		Caller is responsible for disposing the returned stream.
	/// </returns>
	/// <remarks>
	///		The method performs a synchronous open and returns a task-wrapped stream to conform to the async interface.
	///		Throws <see cref="ObjectDisposedException"/> if this instance has been disposed.
	/// </remarks>
	public Task<Stream> OpenReadStreamAsync(CancellationToken ct)
	{
		ThrowIfDisposed();

		// Return a FileStream opened directly from the path configured for async I/O.
		Stream fs = new FileStream(
			FileInfo.FullName,
			FileMode.Open,
			FileAccess.Read,
			FileShare.Read,
			bufferSize,
			FileOptions.Asynchronous | FileOptions.SequentialScan
		);

		return Task.FromResult<Stream>(fs);
	}

	/// <summary>
	///		Checks whether the instance is disposed and throws <see cref="ObjectDisposedException"/> if so.
	///		Protected to allow derived types to reuse the check without exposing the flag.
	/// </summary>
	protected void ThrowIfDisposed()
	{
		// Volatile.Read ensures correct memory ordering for the boolean flag.
		ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed), this);
	}

	/// <summary>
	///		Marks the instance as disposed. Protected so derived types can invalidate the instance (e.g. after a move).
	/// </summary>
	protected void MarkDisposed()
	{
		Volatile.Write(ref _disposed, true);
	}
}
