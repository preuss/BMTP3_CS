using BMTP3.Core4.Engine.TimeStamp.Parsers;

namespace BMTP3.Core4.Tests.Engine.TimeStamp.Parsers;

public class OffsetParserTests
{
    private static readonly OffsetParser Parser = new();

	[Theory]
	[InlineData("Z", 0, 0, 0, 0)]
	[InlineData("+02:00", 2, 0, 0, 0)]
	[InlineData("-05:30", -5, -30, 0, 0)]
	[InlineData("+02:00:00", 2, 0, 0, 0)]
	[InlineData("-05:30:00", -5, -30, 0, 0)]
	[InlineData("+02:00:00.1234567", 2, 0, 0, 1234567)]
	[InlineData("+02", 2, 0, 0, 0)]
	[InlineData("-05", -5, 0, 0, 0)]
	[InlineData("+020000", 2, 0, 0, 0)]
	[InlineData("-053000", -5, -30, 0, 0)]
	[InlineData("02:00", 2, 0, 0, 0)]          // no prefix — parsed as positive
	public void TryParse_ValidInput_ReturnsTimeSpan(string input, int hours, int minutes, int seconds, int expectedSubSecondsTicks)
	{
		var result = Parser.TryParse(input, out TimeSpan ts);
		Assert.True(result);
		long expectedTicks = new TimeSpan(hours, minutes, seconds).Ticks;
		if (hours >= 0)
			Assert.Equal(expectedTicks + expectedSubSecondsTicks, ts.Ticks);
		else
			Assert.Equal(expectedTicks - expectedSubSecondsTicks, ts.Ticks);
	}

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("garbage")]
    [InlineData("ABC")]
    public void TryParse_InvalidInput_ReturnsFalse(string? input)
    {
        Assert.False(Parser.TryParse(input, out TimeSpan _));
    }

    [Theory]
    [InlineData("Z")]
    [InlineData("+02:00")]
    [InlineData("-05:30")]
    public void Parse_ValidInput_ReturnsTimeSpan(string input)
    {
        var result = Parser.Parse(input);
        Assert.IsType<TimeSpan>(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("garbage")]
    public void Parse_InvalidInput_ThrowsFormatException(string? input)
    {
        Assert.Throws<FormatException>(() => Parser.Parse(input!));
    }

    [Theory]
    [InlineData("+02:00", true)]
    [InlineData("garbage", false)]
    [InlineData("", false)]
    public void IsMatch_ReturnsExpected(string input, bool expected)
    {
        Assert.Equal(expected, Parser.IsMatch(input));
    }
}
