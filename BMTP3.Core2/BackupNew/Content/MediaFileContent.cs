using BMTP3.Core2.BackupNew.Infrastructure.Traversal;
using MediaDevices;
using System.Runtime.Versioning;

namespace BMTP3.Core2.BackupNew.Content;
/// <summary>
/// ISourceContent implementation for files on MTP/PTP devices (phones, cameras, etc.).
/// Wraps a MediaFileInfo from the MediaDevices library.
/// </summary>
[SupportedOSPlatform("windows7.0")]
public sealed class MediaFileContent : IContent
{
	private readonly MediaFileInfo _mediaFileInfo;
	private readonly IMtpGatekeeper _gatekeeper;
	private bool _disposed;

	public MediaFileContent(MediaFileInfo mediaFileInfo, IMtpGatekeeper gatekeeper)
	{
		_mediaFileInfo = mediaFileInfo ?? throw new ArgumentNullException(nameof(mediaFileInfo));
		_gatekeeper = gatekeeper ?? throw new ArgumentNullException(nameof(gatekeeper));
	}

	/// <summary>
	/// Gets the size of the file on the device in bytes.
	/// </summary>
	public ulong Length => _mediaFileInfo.Length;

	/// <summary>
	/// Opens a readable stream to the file content on the device.
	/// The caller is responsible for disposing the returned stream.
	/// </summary>
	public Stream OpenRead()
	{
		ObjectDisposedException.ThrowIf(_disposed, nameof(MediaFileContent));

		// We open the raw stream, but wrap it so that every Read() call is gated.
		// Opening the stream itself (sending the command) also needs protection?
		// Usually OpenRead just returns a handle, but let's be safe.
		Stream rawStream = _gatekeeper.ExecuteAsync(() => Task.FromResult(_mediaFileInfo.OpenRead()), CancellationToken.None).GetAwaiter().GetResult();

		return new GatekeptStream(rawStream, _gatekeeper);
	}

	/// <summary>
	/// Opens a readable stream to the file content on the device asynchronously.
	/// <para>
	/// <strong>Warning:</strong> This method uses "Sync-over-Async". The underlying MTP operation is synchronous and blocking.
	/// This method wraps the blocking call in <see cref="Task.Run(Action)"/> to offload it to a ThreadPool thread.
	/// While this unblocks the calling thread, it consumes a ThreadPool thread for the duration of the operation.
	/// High parallelism with this method may lead to ThreadPool starvation.
	/// </para>
	/// The caller is responsible for disposing the returned stream.
	/// </summary>
	public Task<Stream> OpenReadStreamAsync(CancellationToken ct)
	{
		ObjectDisposedException.ThrowIf(_disposed, nameof(MediaFileContent));
		// The MediaDevices library's OpenRead() is blocking, so we wrap it in Task.Run.
		// There's no native async API for MTP devices in MediaDevices currently.
		return Task.Run(() => _mediaFileInfo.OpenRead(), ct);
	}

	public void Dispose()
	{
		_disposed = true;
		// No resources to release — stream is owned by caller
	}
}