using BMTP3.Core4.Engine.TimeStamp.Candidates;
using BMTP3.Core4.Engine.TimeStamp.Parsers;

namespace BMTP3.Core4.Tests.Engine.TimeStamp.Parsers;

public class DateWithYearMonthParserTests
{
    private static readonly DateWithYearMonthParser Parser = new();

    [Theory]
    [InlineData("2024-06", 2024, 6)]
    [InlineData("2024:06", 2024, 6)]
    [InlineData("2024.06", 2024, 6)]
    [InlineData("2024/06", 2024, 6)]
    [InlineData("2024 06", 2024, 6)]
    [InlineData("202406", 2024, 6)]
    [InlineData("0001-01", 1, 1)]
    [InlineData("9999-12", 9999, 12)]
    public void TryParse_ValidInput_ReturnsDate(string input, int year, int month)
    {
        var result = Parser.TryParse(input, out DateOnly date);
        Assert.True(result);
        Assert.Equal(new DateOnly(year, month, 1), date);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("2024-13")]
    [InlineData("2024-06-15")]
    [InlineData("garbage")]
    [InlineData("2024")]
    public void TryParse_InvalidInput_ReturnsFalse(string? input)
    {
        Assert.False(Parser.TryParse(input, out DateOnly _));
    }

    [Theory]
    [InlineData("2024-06")]
    [InlineData("2024:06")]
    [InlineData("9999-12")]
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
    [InlineData("2024-06", true)]
    [InlineData("garbage", false)]
    [InlineData("", false)]
    public void IsMatch_ReturnsExpected(string input, bool expected)
    {
        Assert.Equal(expected, Parser.IsMatch(input));
    }

	[Theory]
	[InlineData("2024-06", 2024, 6)]
	[InlineData(null, 1, 1)]      // ParseOrNull returns default(DateOnly) on failure
	[InlineData("garbage", 1, 1)]
	public void ParseOrNull_ReturnsExpected(string? input, int year, int month)
	{
		DateOnly? result = Parser.ParseOrNull(input);
		Assert.True(result.HasValue);
		Assert.Equal(new DateOnly(year, month, 1), result.Value);
	}

    [Fact]
    public void Resolution_IsYearAndMonth()
    {
        Assert.Equal(ChronoDateResolution.YearAndMonth, Parser.Resolution);
    }
}
