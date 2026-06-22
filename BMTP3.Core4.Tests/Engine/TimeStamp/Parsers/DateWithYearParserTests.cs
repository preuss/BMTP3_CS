using BMTP3.Core4.Engine.TimeStamp.Candidates;
using BMTP3.Core4.Engine.TimeStamp.Parsers;

namespace BMTP3.Core4.Tests.Engine.TimeStamp.Parsers;

public class DateWithYearParserTests
{
    private static readonly DateWithYearParser Parser = new();

    [Theory]
    [InlineData("2024", 2024)]
    [InlineData("0001", 1)]
    [InlineData("9999", 9999)]
    public void TryParse_ValidInput_ReturnsDate(string input, int year)
    {
        var result = Parser.TryParse(input, out DateOnly date);
        Assert.True(result);
        Assert.Equal(new DateOnly(year, 1, 1), date);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("202")]
    [InlineData("2024-06")]
    [InlineData("garbage")]
    public void TryParse_InvalidInput_ReturnsFalse(string? input)
    {
        Assert.False(Parser.TryParse(input, out DateOnly _));
    }

    [Theory]
    [InlineData("2024")]
    [InlineData("0001")]
    [InlineData("9999")]
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
    [InlineData("2024", true)]
    [InlineData("garbage", false)]
    [InlineData("", false)]
    public void IsMatch_ReturnsExpected(string input, bool expected)
    {
        Assert.Equal(expected, Parser.IsMatch(input));
    }

	[Theory]
	[InlineData("2024", 2024)]
	[InlineData(null, 1)]          // ParseOrNull returns default(DateOnly) on failure
	[InlineData("garbage", 1)]
	public void ParseOrNull_ReturnsExpected(string? input, int year)
	{
		DateOnly? result = Parser.ParseOrNull(input);
		Assert.True(result.HasValue);
		Assert.Equal(new DateOnly(year, 1, 1), result.Value);
	}

    [Fact]
    public void Resolution_IsYearOnly()
    {
        Assert.Equal(ChronoDateResolution.YearOnly, Parser.Resolution);
    }
}
