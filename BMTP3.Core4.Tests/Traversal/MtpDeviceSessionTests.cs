using BMTP3.Core4.Traversal;
using MediaDevices;
using System.Runtime.Versioning;

namespace BMTP3.Core4.Tests.Traversal;

[SupportedOSPlatform("windows7.0")]
public class MtpDeviceSessionTests
{
	[Fact]
	public void Open_NullDevice_Throws()
	{
		Assert.Throws<ArgumentNullException>(() => MtpDeviceSession.Open(null!));
	}

	[Fact]
	public void Open_AlreadyConnectedDevice_Throws()
	{
		MediaDevice? device = GetFirstDevice();
		if(device is null)
			return;

		device.Connect();
		try
		{
			Assert.Throws<InvalidOperationException>(() => MtpDeviceSession.Open(device));
		}
		finally
		{
			device.Disconnect();
		}
	}

	[Fact]
	public void Open_ThenDispose_Idempotent()
	{
		MediaDevice? device = GetFirstDevice();
		if(device is null)
			return;

		using IMtpDeviceSession session = MtpDeviceSession.Open(device);
		session.Dispose();
		session.Dispose();
	}

	[Fact]
	public void Open_ThenDispose_ReturnsFriendlyNameBeforeDispose()
	{
		MediaDevice? device = GetFirstDevice();
		if(device is null)
			return;

		using IMtpDeviceSession session = MtpDeviceSession.Open(device);
		Assert.Equal(device.FriendlyName, session.DeviceName);
	}

	private static MediaDevice? GetFirstDevice()
	{
		return MediaDevice.GetDevices().FirstOrDefault();
	}
}