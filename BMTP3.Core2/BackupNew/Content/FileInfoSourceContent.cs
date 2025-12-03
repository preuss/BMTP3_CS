namespace BMTP3.Core2.BackupNew.Content;
/// <summary>
/// ISourceContent implementation that reads from a regular file on disk.
/// </summary>
public sealed class FileInfoSourceContent : ISourceContent
{
	private readonly FileInfo _fileInfo;
	private bool _disposed;

	public FileInfoSourceContent(string filePath) : this(new FileInfo(filePath))
	{
	}
	public FileInfoSourceContent(FileInfo fileInfo)
	{
		_fileInfo = fileInfo ?? throw new ArgumentNullException(nameof(fileInfo));
		if(!_fileInfo.Exists)
		{
			throw new FileNotFoundException($"File not found: {fileInfo.FullName}");
		}
	}

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
}