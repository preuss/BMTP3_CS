using BMTP3.Core4.Traversal;
using MediaDevices;
using System.Runtime.Versioning;

namespace BMTP3.Core4.Tests.Traversal;

[SupportedOSPlatform("windows7.0")]
public class MediaDeviceSessionTests
{
	[Fact]
	public void Open_NullDevice_Throws()
	{
		Assert.Throws<ArgumentNullException>(() => MediaDeviceSession.Open(null!));
	}
}
