using BMTP3.Core2.BackupNew.Engine.Traversal;
using MediaDevices;

namespace BMTP3.Core2.BackupNew.Infrastructure.Traversal;

/// <summary>
/// Default implementation of <see cref="IMediaDeviceScannerFactory"/>.
/// Creates a <see cref="MediaDeviceScanner"/> bound to the supplied connected device.
/// </summary>
public sealed class MediaDeviceScannerFactory : IMediaDeviceScannerFactory
{
	private readonly IMtpGatekeeper _gatekeeper;

	public MediaDeviceScannerFactory(IMtpGatekeeper gatekeeper)
	{
		_gatekeeper = gatekeeper ?? throw new ArgumentNullException(nameof(gatekeeper));
	}

	/// <inheritdoc />
	public ITraversalScanner<MediaFileInfo> Create(MediaDevice connectedDevice)
	{
		ArgumentNullException.ThrowIfNull(connectedDevice);
		return new MediaDeviceScanner(connectedDevice, _gatekeeper);
	}
}
