using BMTP3.Core4.Engine.TimeStamp.Parsers;

namespace BMTP3.Core4.Tests.Engine.TimeStamp.Parsers;

public class TimeParserTests
{
    private static readonly TimeParser Parser = new();

	[Theory]
	[InlineData("14:30:00", 14, 30, 0, 0)]
	[InlineData("14:30:00.1234567", 14, 30, 0, 1234567)]
	[InlineData("14:30:00,9876543", 14, 30, 0, 9876543)]
	[InlineData("14:30", 14, 30, 0, 0)]
	[InlineData("00:00:00", 0, 0, 0, 0)]
	[InlineData("23:59:59", 23, 59, 59, 0)]
	public void TryParse_ValidInput_ReturnsTime(string input, int hour, int minute, int second, int expectedFractionTicks)
	{
		var result = Parser.TryParse(input, out TimeOnly time);
		Assert.True(result);
		long expectedTicks = new TimeOnly(hour, minute, second).Ticks + expectedFractionTicks;
		Assert.Equal(expectedTicks, time.Ticks);
	}

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("25:00:00")]
    [InlineData("14:60:00")]
    [InlineData("garbage")]
    [InlineData("14-30")]
    public void TryParse_InvalidInput_ReturnsFalse(string? input)
    {
        Assert.False(Parser.TryParse(input, out TimeOnly _));
    }

    [Theory]
    [InlineData("14:30:00")]
    [InlineData("14:30:00.1234567")]
    [InlineData("14:30")]
    public void Parse_ValidInput_ReturnsTime(string input)
    {
        var result = Parser.Parse(input);
        Assert.IsType<TimeOnly>(result);
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
    [InlineData("14:30:00", true)]
    [InlineData("garbage", false)]
    [InlineData("", false)]
    public void IsMatch_ReturnsExpected(string input, bool expected)
    {
        Assert.Equal(expected, Parser.IsMatch(input));
    }
}
