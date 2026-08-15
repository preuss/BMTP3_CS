using BMTP3.Consoles.IO.Consoles.Progress.Columns;
using Xunit;

namespace BMTP3.Consoles.Tests;

public class ThroughputColumnTests
{
	[Fact]
	public void FormatThroughput_NullElapsed_Placeholder()
	{
		Assert.Equal("--.- MB/s", ThroughputColumn.FormatThroughput(1000, null));
	}

	[Theory]
	[InlineData(0, 10.0)]    // zero bytes
	[InlineData(1000, 0.5)]  // less than 1 second
	public void FormatThroughput_LowOrZeroRate_Placeholder(long bytes, double seconds)
	{
		Assert.Equal("--.- MB/s", ThroughputColumn.FormatThroughput(bytes, TimeSpan.FromSeconds(seconds)));
	}

	[Fact]
	public void FormatThroughput_UnderOneMegabytePerSecond_ShowsKbPerSecond()
	{
		// 512 * 1024 bytes in 1s = 512 KB/s
		Assert.Equal("512 KB/s", ThroughputColumn.FormatThroughput(512L * 1024, TimeSpan.FromSeconds(1)));
	}

	[Fact]
	public void FormatThroughput_OneMegabyteOrMore_ShowsMbPerSecond()
	{
		// 5 * 1024 * 1024 bytes in 1s = 5.0 MB/s
		Assert.Equal("5.0 MB/s", ThroughputColumn.FormatThroughput(5L * 1024 * 1024, TimeSpan.FromSeconds(1)));
	}
}