using BMTP3.Core2.BackupNew2.Interfaces;

namespace BMTP3.Core2.BackupNew2.Sources;

/// <summary>
/// ISourceContent implementation that reads from a regular file on disk.
/// </summary>
public sealed class FileInfoSourceContent : ISourceContent
{
	private readonly FileInfo _fileInfo;

	public FileInfoSourceContent(FileInfo fileInfo)
	{
		_fileInfo = fileInfo ?? throw new ArgumentNullException(nameof(fileInfo));
	}

	public ulong Length => (ulong)_fileInfo.Length;

	public Stream OpenRead()
	{
		// FileShare.ReadWrite allows other processes (e.g. phone sync tools) to keep the file open
		return new FileStream(
			_fileInfo.FullName,
			FileMode.Open,
			FileAccess.Read,
			FileShare.ReadWrite);
	}

	public void Dispose()
	{
		// No resources to release — stream is owned by caller
	}
}
