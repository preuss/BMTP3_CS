using BMTP3.Core4.Tests.Fakes;
using BMTP3.Core4.Traversal;
using MediaDevices;
using System.Runtime.Versioning;

namespace BMTP3.Core4.Tests.Traversal;

[SupportedOSPlatform("windows7.0")]
public class MediaDeviceTraversalTests
{
	[Fact]
	public void Constructor_NullDevice_Throws()
	{
		Assert.Throws<ArgumentNullException>(() => new MediaDeviceTraversal(null!, new FakeGatekeeper()));
	}
}
