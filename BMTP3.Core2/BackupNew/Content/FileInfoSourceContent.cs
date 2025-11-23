using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core.BackupNew.Content;
/// <summary>
/// ISourceContent implementation that reads from a regular file on disk.
/// </summary>
public sealed class FileInfoSourceContent : ISourceContent {
	private readonly FileInfo _fileInfo;
	private bool _disposed;

	public FileInfoSourceContent(FileInfo fileInfo) {
		_fileInfo = fileInfo ?? throw new ArgumentNullException(nameof(fileInfo));
		if(!_fileInfo.Exists)
			throw new FileNotFoundException($"File not found: {fileInfo.FullName}");
	}

	public ulong Length => (ulong)_fileInfo.Length;

	public Stream OpenRead() {
		// FileShare.ReadWrite allows other processes (e.g. phone sync tools) to keep the file open
		return new FileStream(
			_fileInfo.FullName,
			FileMode.Open,
			FileAccess.Read,
			FileShare.ReadWrite);
	}

	public void Dispose() {
		if(!_disposed) {
			// Nothing to dispose – the stream is owned by the caller
			_disposed = true;
		}
	}
}