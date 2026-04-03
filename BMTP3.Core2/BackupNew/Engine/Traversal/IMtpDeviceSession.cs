using MediaDevices;

namespace BMTP3.Core2.BackupNew.Engine.Traversal;

/// <summary>
/// Represents an active connection to an MTP device.
/// Must be disposed after all content from the device has been read (i.e. after staging completes).
/// Disposing disconnects the device.
/// </summary>
public interface IMtpDeviceSession : IDisposable
{
	/// <summary>The friendly name of the connected device.</summary>
	string DeviceName { get; }
}
