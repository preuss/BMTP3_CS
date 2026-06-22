using BMTP3.Core4.Engine.TimeStamp.Parsers;

namespace BMTP3.Core4.Tests.Engine.TimeStamp.Parsers;

public class SubSecondParserTests
{
    private static readonly SubSecondParser Parser = new();

    [Theory]
    [InlineData("0", 0L)]
    [InlineData("1", 100_000_000L)]
    [InlineData("123", 123_000_000L)]
    [InlineData("123456789", 123_456_789L)]
    [InlineData("100000000", 100_000_000L)]
    [InlineData("999999999", 999_999_999L)]
    [InlineData("000001", 1_000L)]          // len=6, padded to 9 → 1 * 1000 = 1000
    public void TryParse_UpTo9Digits_ScalesToNanoseconds(string input, long expected)
    {
        var result = Parser.TryParse(input, out long ns);
        Assert.True(result);
        Assert.Equal(expected, ns);
    }

	[Theory]
	[InlineData("1234567890", 123_456_789L)]   // 10 digits → truncate last digit (÷10)
	[InlineData("12345678901", 123_456_789L)]   // 11 digits → truncate last 2 digits (÷100)
	[InlineData("1000000000", 100_000_000L)]   // 10 digits → ÷10
	public void TryParse_MoreThan9Digits_TruncatesToNanoseconds(string input, long expected)
	{
		var result = Parser.TryParse(input, out long ns);
		Assert.True(result);
		Assert.Equal(expected, ns);
	}

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("garbage")]
    [InlineData("12.3")]
    [InlineData("abc123")]
    public void TryParse_InvalidInput_ReturnsFalse(string? input)
    {
        Assert.False(Parser.TryParse(input, out long _));
    }

    [Theory]
    [InlineData("123456789")]
    [InlineData("0")]
    [InlineData("999999999")]
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
}
