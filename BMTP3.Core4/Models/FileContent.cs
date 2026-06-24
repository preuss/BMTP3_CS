namespace BMTP3.Core4.Models;

internal class FileContent : IContent, IFileInfoSource
{
	private int _invalidated;
	private const int BufferSize = 128 * 1024; // Default is 4096; 128 KB can improve performance for large sequential reads.

	/// <summary>
	///     Creates a <see cref="FileContent"/> that represents a regular file on disk.
	/// </summary>
	/// <param name="filePath">Full path to the file.</param>
	/// <exception cref="ArgumentNullException"><paramref name="filePath"/> is <c>null</c>.</exception>
	/// <exception cref="FileNotFoundException">The file does not exist.</exception>
	public FileContent(string filePath) : this(new FileInfo(filePath))
	{
	}

	/// <summary>
	///     Creates a <see cref="FileContent"/> from an existing <see cref="FileInfo"/>.
	/// </summary>
	/// <param name="fileInfo">The <see cref="FileInfo"/> describing the file. Must exist.</param>
	/// <exception cref="ArgumentNullException"><paramref name="fileInfo"/> is <c>null</c>.</exception>
	/// <exception cref="FileNotFoundException">The file does not exist.</exception>
	public FileContent(FileInfo fileInfo)
	{
		ArgumentNullException.ThrowIfNull(fileInfo);

		if (!fileInfo.Exists)
		{
			throw new FileNotFoundException($"File not found: {fileInfo.FullName}", fileInfo.FullName);
		}

		FileInfo = fileInfo;
	}

	/// <summary>
	///     The underlying <see cref="FileInfo"/> for this content.
	///     This property is protected so derived types can access file metadata
	///     and perform file operations such as atomic move.
	/// </summary>
	protected FileInfo FileInfo { get; }

	/// <summary>
	///     Length of the underlying file in bytes.
	/// </summary>
	/// <remarks>
	///     Throws <see cref="InvalidOperationException"/> if this instance has been invalidated.
	/// </remarks>
	public ulong Length
	{
		get
		{
			ThrowIfInvalidated();
			return (ulong)FileInfo.Length;
		}
	}

	/// <summary>
	///     Tries to expose the underlying <see cref="FileInfo"/>.
	/// </summary>
	/// <param name="fileInfo">When this method returns, contains the underlying <see cref="FileInfo"/>.</param>
	/// <returns><c>true</c> for file-backed content.</returns>
	/// <remarks>
	///     Throws <see cref="InvalidOperationException"/> if this instance has been invalidated.
	/// </remarks>
	public bool TryGetFileInfo(out FileInfo fileInfo)
	{
		ThrowIfInvalidated();
		fileInfo = FileInfo;
		return true;
	}

	/// <summary>
	///     Opens a synchronous <see cref="Stream"/> for reading.
	/// </summary>
	/// <returns>
	///     An open <see cref="FileStream"/> for reading.
	///     The caller is responsible for disposing the returned stream.
	/// </returns>
	/// <exception cref="InvalidOperationException">This instance has been invalidated.</exception>
	public Stream OpenRead()
	{
		ThrowIfInvalidated();

		return new FileStream(
			FileInfo.FullName,
			FileMode.Open,
			FileAccess.Read,
			FileShare.Read,
			BufferSize);
	}

	/// <summary>
	///     Opens a <see cref="Stream"/> configured for asynchronous read operations.
	/// </summary>
	/// <param name="ct">
	///     Cancellation token to cancel the operation.
	/// </param>
	/// <returns>
	///     A <see cref="Task{TResult}"/> that returns an open <see cref="FileStream"/>
	///     configured with <see cref="FileOptions.Asynchronous"/> and
	///     <see cref="FileOptions.SequentialScan"/>.
	///     The caller is responsible for disposing the returned stream.
	/// </returns>
	/// <remarks>
	///     The open itself is still synchronous; the returned stream is simply configured
	///     for efficient asynchronous reads.
	/// </remarks>
	/// <exception cref="InvalidOperationException">This instance has been invalidated.</exception>
	public Task<Stream> OpenReadAsync(CancellationToken ct)
	{
		ThrowIfInvalidated();

		Stream fs = new FileStream(
			FileInfo.FullName,
			FileMode.Open,
			FileAccess.Read,
			FileShare.Read,
			BufferSize,
			FileOptions.Asynchronous | FileOptions.SequentialScan);

		return Task.FromResult(fs);
	}

	/// <summary>
	///     Throws if this content instance has been invalidated and must no longer be used.
	/// </summary>
	/// <remarks>
	///     Protected so derived types can reuse the guard consistently.
	/// </remarks>
	protected void ThrowIfInvalidated()
	{
		if (Volatile.Read(ref _invalidated) != 0)
		{
			throw new InvalidOperationException("This content instance is no longer valid.");
		}
	}

	/// <summary>
	///     Invalidates this instance so that subsequent operations fail.
	/// </summary>
	/// <remarks>
	///     Intended for derived types that perform state-changing operations,
	///     such as moving the underlying file and transferring ownership to a new instance.
	/// </remarks>
	protected void Invalidate()
	{
		Interlocked.Exchange(ref _invalidated, 1);
	}
}