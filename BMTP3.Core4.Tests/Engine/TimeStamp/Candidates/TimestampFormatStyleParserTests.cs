using BMTP3.Core4.Engine.TimeStamp.Candidates;

namespace BMTP3.Core4.Tests.Engine.TimeStamp.Candidates;

public class TimestampFormatStyleParserTests
{
    private const TimestampFormatStyle Fallback = TimestampFormatStyle.Iso8601_DotFraction;

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ParseOrDefault_NullOrEmpty_ReturnsFallback(string? format)
    {
        Assert.Equal(Fallback, TimestampFormatStyleParser.ParseOrDefault(format, Fallback));
    }

    [Theory]
    [InlineData("o")]
    [InlineData("O")]
    [InlineData("iso")]
    public void ParseOrDefault_Oiso_ReturnsIso8601DotFraction(string format)
    {
        Assert.Equal(TimestampFormatStyle.Iso8601_DotFraction, TimestampFormatStyleParser.ParseOrDefault(format, Fallback));
    }

    [Theory]
    [InlineData("r")]
    [InlineData("R")]
    public void ParseOrDefault_R_ReturnsIso8601CommaFraction(string format)
    {
        Assert.Equal(TimestampFormatStyle.Iso8601_CommaFraction, TimestampFormatStyleParser.ParseOrDefault(format, Fallback));
    }

    [Fact]
    public void ParseOrDefault_Compact_ReturnsCompactDotFraction()
    {
        Assert.Equal(TimestampFormatStyle.Compact_DotFraction, TimestampFormatStyleParser.ParseOrDefault("compact", Fallback));
    }

    [Theory]
    [InlineData("  ")]
    [InlineData("\t")]
    public void ParseOrDefault_Whitespace_ReturnsFallback(string format)
    {
        Assert.Equal(Fallback, TimestampFormatStyleParser.ParseOrDefault(format, Fallback));
    }

    [Theory]
    [InlineData("Iso8601_DotFraction")]
    [InlineData("iso8601_dotfraction")]
    [InlineData("Compact_CommaFraction")]
    [InlineData("compact_underscore_dotfraction")]
    [InlineData("DotDateTime_DotFraction")]
    [InlineData("DanishSeparator_Underscore_CommaFraction")]
    public void ParseOrDefault_EnumName_ReturnsMatchingStyle(string format)
    {
        var expected = Enum.Parse<TimestampFormatStyle>(format, ignoreCase: true);
        Assert.Equal(expected, TimestampFormatStyleParser.ParseOrDefault(format, Fallback));
    }

    [Theory]
    [InlineData("garbage")]
    [InlineData("unknown")]
    [InlineData("xyz")]
    public void ParseOrDefault_Unrecognized_ReturnsFallback(string format)
    {
        Assert.Equal(Fallback, TimestampFormatStyleParser.ParseOrDefault(format, Fallback));
    }

    [Fact]
    public void ParseOrDefault_CaseInsensitive_Lowercase()
    {
        Assert.Equal(TimestampFormatStyle.Compact_CommaFraction, TimestampFormatStyleParser.ParseOrDefault("compact_commafraction", Fallback));
    }
}
