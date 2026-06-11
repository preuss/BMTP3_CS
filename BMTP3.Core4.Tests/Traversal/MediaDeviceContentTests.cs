using BMTP3.Core4.Models;
using BMTP3.Core4.Tests.Fakes;
using BMTP3.Core4.Traversal;
using MediaDevices;
using System.Runtime.Versioning;

namespace BMTP3.Core4.Tests.Traversal;

[SupportedOSPlatform("windows7.0")]
public class MediaDeviceContentTests
{
	[Fact]
	public void Constructor_NullMediaFileInfo_Throws()
	{
		var gatekeeper = new FakeGatekeeper();
		Assert.Throws<ArgumentNullException>(() => new MediaDeviceContent(null!, gatekeeper));
	}
}
