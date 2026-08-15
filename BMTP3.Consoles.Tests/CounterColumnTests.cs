using BMTP3.Consoles.IO.Consoles.Progress.Columns;
using Xunit;

namespace BMTP3.Consoles.Tests;

public class CounterColumnTests
{
	[Fact]
	public void RenderText_EscapesMarkup()
	{
		string result = CounterColumn.RenderText("photos [2021].jpg");
		Assert.Equal("photos [[2021]].jpg", result);
	}

	[Fact]
	public void RenderText_RemovesNewLinesAndTrims()
	{
		Assert.Equal("line1line2", CounterColumn.RenderText("  line1\nline2  "));
	}

	[Fact]
	public void RenderText_Null_ReturnsEmpty()
	{
		Assert.Equal("", CounterColumn.RenderText(null));
	}
}