using BMTP3.Core4.Engine.TimeStamp.Parsers;

namespace BMTP3.Core4.Tests.Engine.TimeStamp.Parsers;

public class DateTimeParserTests
{
    private static readonly DateTimeParser Parser = new();

    private static readonly DateTime ExpectedBase = new(2024, 6, 15, 14, 30, 0);

    [Theory]
    [InlineData("2024:06:15 14:30:00")]
    [InlineData("2024-06-15 14:30:00")]
    [InlineData("2024.06.15 14:30:00")]
    [InlineData("2024/06/15 14:30:00")]
    [InlineData("2024 06 15 14:30:00")]
    [InlineData("2024:06:15T14:30:00")]
    [InlineData("2024-06-15T14:30:00")]
    [InlineData("2024.06.15T14:30:00")]
    [InlineData("2024/06/15T14:30:00")]
    [InlineData("2024 06 15T14:30:00")]
    public void TryParse_DateTime_NoFraction_ReturnsExpected(string input)
    {
        Assert.True(Parser.TryParse(input, out DateTime result));
        Assert.Equal(ExpectedBase, result);
        Assert.Equal(DateTimeKind.Unspecified, result.Kind);
    }

	[Theory]
	[InlineData("2024-06-15 14:30:00.1234567", 1234567)]
	[InlineData("2024-06-15 14:30:00,9876543", 9876543)]
	[InlineData("2024-06-15T14:30:00.1234567", 1234567)]
	[InlineData("2024-06-15T14:30:00,9876543", 9876543)]
	[InlineData("20240615 14:30:00.1234567", 1234567)]
	[InlineData("20240615T14:30:00,9876543", 9876543)]
	public void TryParse_DateTime_WithFraction_ReturnsExpected(string input, int expectedTicksFraction)
	{
		Assert.True(Parser.TryParse(input, out DateTime result));
		Assert.Equal(2024, result.Year);
		Assert.Equal(6, result.Month);
		Assert.Equal(15, result.Day);
		Assert.Equal(14, result.Hour);
		Assert.Equal(30, result.Minute);
		Assert.Equal(0, result.Second);
		long fractionTicks = result.Ticks % TimeSpan.TicksPerSecond;
		Assert.Equal(expectedTicksFraction, fractionTicks);
	}

    [Theory]
    [InlineData("2024-06-15 14:30")]
    [InlineData("2024-06-15 14:30:00")]
    [InlineData("2024-06-15 14:30:00.1234567")]
    [InlineData("2024-06-15T14:30")]
    [InlineData("2024-06-15T14:30:00")]
    [InlineData("2024-06-15T14:30:00.1234567")]
    [InlineData("20240615 14:30")]
    [InlineData("20240615T14:30")]
    [InlineData("20240615 14:30:00")]
    [InlineData("20240615T14:30:00")]
    [InlineData("20240615 14:30:00.1234567")]
    [InlineData("20240615T14:30:00.1234567")]
    public void TryParse_DateTime_AllPrecisions_ReturnsExpected(string input)
    {
        Assert.True(Parser.TryParse(input, out DateTime _));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("garbage")]
    [InlineData("2024-06-15")]       // date only, no time
    [InlineData("14:30:00")]          // time only, no date
    [InlineData("2024-06-15 14:30:00+02:00")] // offset not supported
    public void TryParse_InvalidInput_ReturnsFalse(string? input)
    {
        Assert.False(Parser.TryParse(input, out DateTime _));
    }

    [Theory]
    [InlineData("2024-06-15 14:30:00")]
    [InlineData("2024-06-15T14:30:00.1234567")]
    public void Parse_ValidInput_ReturnsDateTime(string input)
    {
        var result = Parser.Parse(input);
        Assert.IsType<DateTime>(result);
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
    [InlineData("2024-06-15 14:30:00", true)]
    [InlineData("garbage", false)]
    [InlineData("", false)]
    public void IsMatch_ReturnsExpected(string input, bool expected)
    {
        Assert.Equal(expected, Parser.IsMatch(input));
    }
}
