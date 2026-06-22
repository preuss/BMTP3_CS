using BMTP3.Core4.Engine.TimeStamp.Parsers;

namespace BMTP3.Core4.Tests.Engine.TimeStamp.Parsers;

public class TimestampParserTests
{
    private static readonly TimestampParser Parser = new();

    [Theory]
    [InlineData("0", 0L)]
    [InlineData("1234567890", 1_234_567_890L)]
    [InlineData("1700000000", 1_700_000_000L)]
    [InlineData("  1700000000  ", 1_700_000_000L)]
    [InlineData("+123", 123L)]
    [InlineData("-1", -1L)]
    [InlineData("-1700000000", -1_700_000_000L)]
	[InlineData("9223372036854775807", long.MaxValue)]
    public void TryParse_ValidInput_ReturnsLong(string input, long expected)
    {
        var result = Parser.TryParse(input, out long ts);
        Assert.True(result);
        Assert.Equal(expected, ts);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12.34")]
    [InlineData("garbage")]
    [InlineData("0x1F")]
    [InlineData("1,000")]
    public void TryParse_InvalidInput_ReturnsFalse(string? input)
    {
        Assert.False(Parser.TryParse(input, out long _));
    }

    [Theory]
    [InlineData("1700000000")]
    [InlineData("0")]
    [InlineData("-1")]
    public void Parse_ValidInput_ReturnsLong(string input)
    {
        var result = Parser.Parse(input);
        Assert.IsType<long>(result);
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
    [InlineData("1700000000", true)]
    [InlineData("garbage", false)]
    [InlineData("", false)]
    public void IsMatch_ReturnsExpected(string input, bool expected)
    {
        Assert.Equal(expected, Parser.IsMatch(input));
    }
}
