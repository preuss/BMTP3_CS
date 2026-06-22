using BMTP3.Core4.Engine.TimeStamp.Parsers;

namespace BMTP3.Core4.Tests.Engine.TimeStamp.Parsers;

public class DateTimeOffsetParserTests
{
    private static readonly DateTimeOffsetParser Parser = new();

    [Theory]
    [InlineData("2024-06-15 14:30:00+02:00", 2, 0)]
    [InlineData("2024:06:15 14:30:00+02:00", 2, 0)]
    [InlineData("2024.06.15 14:30:00+02:00", 2, 0)]
    [InlineData("2024/06/15 14:30:00+02:00", 2, 0)]
    [InlineData("2024 06 15 14:30:00+02:00", 2, 0)]
    [InlineData("2024-06-15T14:30:00+02:00", 2, 0)]
    [InlineData("2024-06-15 14:30:00-05:30", -5, -30)]
    [InlineData("2024-06-15 14:30:00+00:00", 0, 0)]
    public void TryParse_WithOffset_ReturnsExpected(string input, int offsetHours, int offsetMinutes)
    {
        Assert.True(Parser.TryParse(input, out DateTimeOffset result));
        Assert.Equal(2024, result.Year);
        Assert.Equal(6, result.Month);
        Assert.Equal(15, result.Day);
        Assert.Equal(14, result.Hour);
        Assert.Equal(30, result.Minute);
        Assert.Equal(new TimeSpan(offsetHours, offsetMinutes, 0), result.Offset);
    }

    [Theory]
    [InlineData("2024-06-15 14:30:00.1234567+02:00")]
    [InlineData("2024-06-15 14:30:00,9876543+02:00")]
    [InlineData("2024-06-15T14:30:00.1234567+02:00")]
    [InlineData("2024-06-15T14:30:00,9876543+02:00")]
    [InlineData("20240615 14:30:00.1234567+02:00")]
    [InlineData("20240615T14:30:00.1234567+02:00")]
    public void TryParse_WithFractionAndOffset_ReturnsExpected(string input)
    {
        Assert.True(Parser.TryParse(input, out DateTimeOffset _));
    }

    [Theory]
    [InlineData("2024-06-15 14:30:00Z")]        // K format — UTC
    [InlineData("2024-06-15T14:30:00Z")]
    [InlineData("2024-06-15 14:30:00.1234567Z")]
    [InlineData("2024-06-15T14:30:00.1234567Z")]
    public void TryParse_ZuluOffset_ReturnsUtc(string input)
    {
        Assert.True(Parser.TryParse(input, out DateTimeOffset result));
        Assert.Equal(DateTimeOffset.UtcNow.Offset, result.Offset);
    }

    [Theory]
    [InlineData("2024-06-15 14:30:00+02:00")]
    [InlineData("2024-06-15 14:30:00-05:30")]
    [InlineData("2024-06-15 14:30:00Z")]
    [InlineData("2024-06-15 14:30:00.1234567+02:00")]
    [InlineData("2024-06-15T14:30:00+02:00")]
    [InlineData("2024-06-15T14:30:00Z")]
    [InlineData("20240615 14:30:00+02:00")]
    [InlineData("20240615T14:30:00+02:00")]
    [InlineData("20240615 14:30:00Z")]
    [InlineData("20240615T14:30:00Z")]
    public void TryParse_AllOffsetVariants_ReturnsExpected(string input)
    {
        Assert.True(Parser.TryParse(input, out DateTimeOffset _));
    }

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("garbage")]
	[InlineData("2024-06-15")]                // date only
	public void TryParse_InvalidInput_ReturnsFalse(string? input)
	{
		Assert.False(Parser.TryParse(input, out DateTimeOffset _));
	}

    [Theory]
    [InlineData("2024-06-15 14:30:00+02:00")]
    [InlineData("2024-06-15T14:30:00Z")]
    public void Parse_ValidInput_ReturnsDateTimeOffset(string input)
    {
        var result = Parser.Parse(input);
        Assert.IsType<DateTimeOffset>(result);
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
    [InlineData("2024-06-15 14:30:00+02:00", true)]
    [InlineData("garbage", false)]
    [InlineData("", false)]
    public void IsMatch_ReturnsExpected(string input, bool expected)
    {
        Assert.Equal(expected, Parser.IsMatch(input));
    }
}
