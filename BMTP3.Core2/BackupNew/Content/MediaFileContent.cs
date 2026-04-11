using System.Runtime.Versioning;
using BMTP3.Core2.BackupNew.Engine.Traversal;
using MediaDevices;

namespace BMTP3.Core2.BackupNew.Content;

/// <summary>
///     <see cref="IContent" /> implementation for files on MTP/PTP devices (phones, cameras, etc.).
///     Wraps a <see cref="MediaFileInfo" /> from the MediaDevices library.
/// </summary>
[SupportedOSPlatform("windows7.0")]
public sealed class MediaFileContent : IContent
{
	private readonly IMtpGatekeeper _gatekeeper;
	private readonly MediaFileInfo _mediaFileInfo;
	private bool _disposed;

	public MediaFileContent(MediaFileInfo mediaFileInfo, IMtpGatekeeper gatekeeper)
	{
		_mediaFileInfo = mediaFileInfo ?? throw new ArgumentNullException(nameof(mediaFileInfo));
		_gatekeeper = gatekeeper ?? throw new ArgumentNullException(nameof(gatekeeper));
	}

	/// <summary>
	///     Gets the size of the file on the device in bytes.
	/// </summary>
	public ulong Length => _mediaFileInfo.Length;

	/// <summary>
	///     Opens a readable stream to the file content on the MTP device.
	///     The MTP gatekeeper semaphore is acquired <em>once</em> here and released only when
	///     the returned stream is disposed.  This means every byte of the file transfer runs
	///     under a single semaphore hold — eliminating the per-chunk acquire/release overhead
	///     that the previous design incurred.
	///     The caller is responsible for disposing the returned stream (which also releases the
	///     semaphore lease).
	/// </summary>
	public Stream OpenRead()
	{
		ObjectDisposedException.ThrowIf(_disposed, nameof(MediaFileContent));

		// Acquire the semaphore lease with timeout. The timeout is configured in MtpGatekeeper
		// via BackupEngineOptions.MtpOperationTimeoutMs (default 60 seconds).
		// Using a linked CTS allows the caller's CancellationToken to also trigger cancellation.
		IDisposable lease = _gatekeeper.AcquireAsync(CancellationToken.None).GetAwaiter().GetResult();
		try
		{
			Stream rawStream = _mediaFileInfo.OpenRead();
			// GatekeptStream owns both the raw stream and the lease; disposing it releases both.
			return new GatekeptStream(rawStream, lease);
		}
		catch
		{
			// If OpenRead throws, release the lease immediately so the semaphore is not abandoned.
			lease.Dispose();
			throw;
		}
	}

	/// <summary>
	///     Opens a readable stream to the file content on the MTP device asynchronously.
	///     The gatekeeper semaphore is acquired once via <see cref="IMtpGatekeeper.AcquireAsync" />
	///     and held for the lifetime of the returned stream.  The underlying
	///     <see cref="MediaFileInfo.OpenRead" /> call is synchronous (no async MTP API exists in
	///     MediaDevices), but the semaphore wait itself is async, avoiding a blocking wait on the
	///     calling thread.
	///     The caller is responsible for disposing the returned stream.
	/// </summary>
	public async Task<Stream> OpenReadStreamAsync(CancellationToken ct)
	{
		ObjectDisposedException.ThrowIf(_disposed, nameof(MediaFileContent));

		IDisposable lease = await _gatekeeper.AcquireAsync(ct);
		try
		{
			Stream rawStream = _mediaFileInfo.OpenRead();
			return new GatekeptStream(rawStream, lease);
		}
		catch
		{
			lease.Dispose();
			throw;
		}
	}

	public void Dispose()
	{
		_disposed = true;
		// No resources to release here — the stream (and its lease) is owned by the caller.
	}
}