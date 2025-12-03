using BMTP3.Core2.BackupNew2.Interfaces;
using MediaDevices;
using System.Runtime.Versioning;

namespace BMTP3.Core2.BackupNew2.Sources;

/// <summary>
/// ISourceContent implementation for files on MTP/PTP devices (phones, cameras, etc.).
/// Wraps a MediaFileInfo from the MediaDevices library.
/// </summary>
[SupportedOSPlatform("windows7.0")]
public sealed class MediaFileSourceContent : ISourceContent
{
	private readonly MediaFileInfo _mediaFileInfo;
	private bool _disposed;

	public MediaFileSourceContent(MediaFileInfo mediaFileInfo)
	{
		_mediaFileInfo = mediaFileInfo ?? throw new ArgumentNullException(nameof(mediaFileInfo));
	}

	public ulong Length => (ulong)_mediaFileInfo.Length;

	public Stream OpenRead()
	{
		if(_disposed) throw new ObjectDisposedException(nameof(MediaFileSourceContent));
		return _mediaFileInfo.OpenRead();
	}

	public void Dispose()
	{
		_disposed = true;
	}
}
