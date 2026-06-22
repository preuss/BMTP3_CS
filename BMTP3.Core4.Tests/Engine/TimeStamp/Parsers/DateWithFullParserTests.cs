using BMTP3.Core4.Engine.TimeStamp.Candidates;
using BMTP3.Core4.Engine.TimeStamp.Parsers;

namespace BMTP3.Core4.Tests.Engine.TimeStamp.Parsers;

public class DateWithFullParserTests
{
    private static readonly DateWithFullParser Parser = new();

    [Theory]
    [InlineData("2024-06-15", 2024, 6, 15)]
    [InlineData("2024:06:15", 2024, 6, 15)]
    [InlineData("2024.06.15", 2024, 6, 15)]
    [InlineData("2024/06/15", 2024, 6, 15)]
    [InlineData("2024 06 15", 2024, 6, 15)]
    [InlineData("20240615", 2024, 6, 15)]
    [InlineData("0001-01-01", 1, 1, 1)]
    [InlineData("9999-12-31", 9999, 12, 31)]
    [InlineData("2024-02-29", 2024, 2, 29)]
    public void TryParse_ValidInput_ReturnsDate(string input, int year, int month, int day)
    {
        var result = Parser.TryParse(input, out DateOnly date);
        Assert.True(result);
        Assert.Equal(new DateOnly(year, month, day), date);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("2024-13-01")]
    [InlineData("2024-06-32")]
    [InlineData("garbage")]
    [InlineData("2024-06")]
    [InlineData("2024")]
    public void TryParse_InvalidInput_ReturnsFalse(string? input)
    {
        Assert.False(Parser.TryParse(input, out DateOnly _));
    }

    [Theory]
    [InlineData("2024-06-15")]
    [InlineData("2024:06:15")]
    [InlineData("9999-12-31")]
    public void Parse_ValidInput_ReturnsDate(string input)
    {
        var date = Parser.Parse(input);
        Assert.IsType<DateOnly>(date);
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
    [InlineData("2024-06-15", true)]
    [InlineData("garbage", false)]
    [InlineData("", false)]
    public void IsMatch_ReturnsExpected(string input, bool expected)
    {
        Assert.Equal(expected, Parser.IsMatch(input));
    }

	[Theory]
	[InlineData("2024-06-15", 2024, 6, 15)]
	[InlineData(null, 1, 1, 1)]   // ParseOrNull returns default(DateOnly) on failure
	[InlineData("garbage", 1, 1, 1)]
	public void ParseOrNull_ReturnsExpected(string? input, int year, int month, int day)
	{
		DateOnly? result = Parser.ParseOrNull(input);
		Assert.True(result.HasValue);
		Assert.Equal(new DateOnly(year, month, day), result.Value);
	}

    [Fact]
    public void Resolution_IsFullDate()
    {
        Assert.Equal(ChronoDateResolution.FullDate, Parser.Resolution);
    }
}
