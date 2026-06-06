using BMTP3.Core4.Traversal;
using MediaDevices;
using System.Runtime.Versioning;

namespace BMTP3.Core4.Models;

[SupportedOSPlatform("windows7.0")]
internal sealed class MediaDeviceContent : IContent
{
	private readonly MediaFileInfo _mediaFileInfo;
	private readonly IMtpGatekeeper _gatekeeper;

	public MediaDeviceContent(MediaFileInfo mediaFileInfo, IMtpGatekeeper gatekeeper)
	{
		_mediaFileInfo = mediaFileInfo ?? throw new ArgumentNullException(nameof(mediaFileInfo));
		_gatekeeper = gatekeeper ?? throw new ArgumentNullException(nameof(gatekeeper));
	}

	public ulong Length => _mediaFileInfo.Length;

	public Stream OpenRead()
	{
		IDisposable lease = _gatekeeper.Acquire(CancellationToken.None);
		return OpenReadCore(lease);
	}

	public async Task<Stream> OpenReadAsync(CancellationToken ct)
	{
		IDisposable lease = await _gatekeeper.AcquireAsync(ct).ConfigureAwait(false);
		return OpenReadCore(lease);
	}

	private Stream OpenReadCore(IDisposable lease)
	{
		try
		{
			Stream rawStream = _mediaFileInfo.OpenRead();
			return new GatekeptStream(rawStream, lease);
		} catch
		{
			lease.Dispose();
			throw;
		}
	}
}