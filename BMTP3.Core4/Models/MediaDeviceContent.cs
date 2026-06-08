using BMTP3.Core4.Devices;
using BMTP3.Core4.Traversal;
using System.Runtime.Versioning;

namespace BMTP3.Core4.Models;

[SupportedOSPlatform("windows7.0")]
internal sealed class MediaDeviceContent : IContent
{
	private readonly IMediaFile _mediaFile;
	private readonly IMediaDeviceGatekeeper _gatekeeper;

	public MediaDeviceContent(IMediaFile mediaFile, IMediaDeviceGatekeeper gatekeeper)
	{
		_mediaFile = mediaFile ?? throw new ArgumentNullException(nameof(mediaFile));
		_gatekeeper = gatekeeper ?? throw new ArgumentNullException(nameof(gatekeeper));
	}

	public ulong Length => _mediaFile.Length;

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
			Stream rawStream = _mediaFile.OpenRead();
			return new GatekeptStream(rawStream, lease);
		} catch
		{
			lease.Dispose();
			throw;
		}
	}
}
