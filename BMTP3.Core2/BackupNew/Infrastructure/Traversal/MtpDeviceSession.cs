using BMTP3.Core2.BackupNew.Engine.Traversal;
using MediaDevices;
using Microsoft.Extensions.Logging;

namespace BMTP3.Core2.BackupNew.Infrastructure.Traversal;

/// <summary>
///     Holds an active MTP device connection open.
///     Disposing this object disconnects the device.
///     BackupEngine obtains this from BackupScanner and disposes it only after
///     ContentBufferingPipelineStage has finished reading all file content.
/// </summary>
internal sealed class MtpDeviceSession : IMtpDeviceSession
{
	private readonly MediaDevice _device;
	private readonly ILogger? _logger;
	private bool _disposed;

	public MtpDeviceSession(MediaDevice device, ILogger? logger = null)
	{
		_device = device ?? throw new ArgumentNullException(nameof(device));
		_logger = logger;
	}

	public string DeviceName => _device.FriendlyName;

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		try
		{
			_device.Disconnect();
			_logger?.LogInformation("Disconnected from MTP device '{DeviceName}'.", _device.FriendlyName);
		}
		catch (Exception ex)
		{
			_logger?.LogWarning(ex, "Exception while disconnecting from MTP device '{DeviceName}'.",
				_device.FriendlyName);
		}
	}
}