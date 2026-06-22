using BMTP3.Core4.Engine.TimeStamp.Candidates;

namespace BMTP3.Core4.Tests.Engine.TimeStamp.Candidates;

public class TimestampSourcesTests
{
    [Fact]
    public void Empty_Singleton_IsEmpty()
    {
        Assert.True(TimestampSources.Empty.IsEmpty);
    }

    [Fact]
    public void Empty_Singleton_IsAllNull()
    {
        Assert.True(TimestampSources.Empty.IsAllNull);
    }

    [Fact]
    public void Default_IsEmpty()
    {
        var sources = new TimestampSources();
        Assert.True(sources.IsEmpty);
    }

    [Fact]
    public void AllNullFields_IsAllNull()
    {
        var sources = new TimestampSources();
        Assert.True(sources.IsAllNull);
        Assert.True(sources.IsEmpty);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void EmptyOrWhitespaceField_IsNotAllNull(string value)
    {
        var sources = new TimestampSources { Date = value };
        Assert.False(sources.IsAllNull);
        Assert.True(sources.IsEmpty);
    }

    [Fact]
    public void SingleFieldSet_IsNotEmpty()
    {
        var sources = new TimestampSources { Date = "2000:01:01" };
        Assert.False(sources.IsEmpty);
        Assert.False(sources.IsAllNull);
    }

    [Theory]
    [InlineData("2000:01:01")]
    [InlineData("12:00:00")]
    [InlineData("+02:00")]
    [InlineData("500")]
    [InlineData("2000:01:01 12:00:00")]
    [InlineData("2000:01:01 12:00:00+02:00")]
    [InlineData("1647416932")]
    public void NonEmptyField_IsNotIsAllNull(string value)
    {
        var sources = new TimestampSources { Date = value };
        Assert.False(sources.IsAllNull);
    }

    [Fact]
    public void Normalize_Null_ReturnsEmpty()
    {
        var result = TimestampSources.Normalize(null);
        Assert.Same(TimestampSources.Empty, result);
    }

    [Fact]
    public void Normalize_AllWhitespace_ReturnsEmpty()
    {
        var sources = new TimestampSources { Date = "  ", Time = "  " };
        var result = TimestampSources.Normalize(sources);
        Assert.Same(TimestampSources.Empty, result);
    }

    [Fact]
    public void Normalize_TrimsWhitespace()
    {
        var sources = new TimestampSources { Date = "  2000:01:01  " };
        var result = TimestampSources.Normalize(sources);
        Assert.Equal("2000:01:01", result.Date);
    }

    [Fact]
    public void Normalize_NonEmpty_ReturnsPopulatedInstance()
    {
        var sources = new TimestampSources { Date = "2000:01:01" };
        var result = TimestampSources.Normalize(sources);
        Assert.Equal("2000:01:01", result.Date);
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void Instance_Normalize_ReturnsPopulatedInstance()
    {
        var sources = new TimestampSources { Date = "2000:01:01" };
        var result = sources.Normalize();
        Assert.Equal("2000:01:01", result.Date);
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void Normalize_MixedWhitespace_ClearsEmptyFields()
    {
        var sources = new TimestampSources { Date = "2000:01:01", Time = "  " };
        var result = TimestampSources.Normalize(sources);
        Assert.Equal("2000:01:01", result.Date);
        Assert.Null(result.Time);
    }

    [Fact]
    public void ToString_AllNull_ReturnsSourcesEmpty()
    {
        Assert.Equal("<Sources/>", new TimestampSources().ToString());
    }

    [Fact]
    public void ToString_WithDate_IncludesDateTag()
    {
        var sources = new TimestampSources { Date = "2000:01:01" };
        Assert.Contains("<Date>2000:01:01</Date>", sources.ToString());
    }

    [Fact]
    public void ToString_MultipleFields_IncludesAllTags()
    {
        var sources = new TimestampSources { Date = "2000:01:01", Time = "12:00:00", Offset = "+02:00" };
        var str = sources.ToString();
        Assert.Contains("<Date>2000:01:01</Date>", str);
        Assert.Contains("<Time>12:00:00</Time>", str);
        Assert.Contains("<Offset>+02:00</Offset>", str);
    }

    [Fact]
    public void ToDebugString_ContainsAllFields()
    {
        var sources = new TimestampSources { Date = "2000:01:01", Time = "12:00:00" };
        var str = sources.ToDebugString();
        Assert.Contains("Date=2000:01:01", str);
        Assert.Contains("Time=12:00:00", str);
    }

    [Fact]
    public void ToDebugString_AllNull_ShowsEmpty()
    {
        var str = new TimestampSources().ToDebugString();
        Assert.Contains("Date=∅", str);
        Assert.Contains("Time=∅", str);
    }
}
