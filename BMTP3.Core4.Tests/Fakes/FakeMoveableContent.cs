using BMTP3.Core4.Models;
using System.Text;

namespace BMTP3.Core4.Tests.Fakes;

internal sealed class FakeMoveableContent : IMoveableContent
{
	private readonly FileInfo _tempFile;
	private readonly byte[] _data;
	private bool _disposed;

	public FakeMoveableContent(string text)
	{
		_data = Encoding.UTF8.GetBytes(text);
		Length = (ulong)_data.Length;

		string tempPath = Path.GetTempFileName();
		File.WriteAllBytes(tempPath, _data);
		_tempFile = new FileInfo(tempPath);
	}

	public ulong Length { get; }

	public void Dispose()
	{
		if(!_disposed)
		{
			_disposed = true;
			if(_tempFile.Exists)
			{
				try { _tempFile.Delete(); } catch { }
			}
		}
	}

	public ValueTask DisposeAsync()
	{
		Dispose();
		return ValueTask.CompletedTask;
	}

	public Stream OpenRead()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		_tempFile.Refresh();
		return _tempFile.OpenRead();
	}

	public Task<Stream> OpenReadAsync(CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();
		return Task.FromResult(OpenRead());
	}

	public bool TryGetFileInfo(out FileInfo fileInfo)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		_tempFile.Refresh();
		fileInfo = _tempFile;
		return true;
	}

	public IContent MoveTo(string destinationPath, bool overwrite)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);

		if(!_tempFile.Exists)
			throw new FileNotFoundException($"Temp file not found: {_tempFile.FullName}");

		string? dir = Path.GetDirectoryName(destinationPath);
		if(!string.IsNullOrEmpty(dir))
			Directory.CreateDirectory(dir);

		if(overwrite && File.Exists(destinationPath))
			File.Delete(destinationPath);

		File.Move(_tempFile.FullName, destinationPath);
		_disposed = true;

		return new FakeContent(Encoding.UTF8.GetString(_data));
	}
}
