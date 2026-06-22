using BMTP3.Core4.Engine.TimeStamp.Candidates;

namespace BMTP3.Core4.Tests.Engine.TimeStamp.Candidates;

public class ParsedTests
{
    [Fact]
    public void Constructor_SetsValueAndRaw()
    {
        var parsed = new Parsed<string>("2024-06-15", "2024:06:15");
        Assert.Equal("2024-06-15", parsed.Value);
        Assert.Equal("2024:06:15", parsed.Raw);
    }

    [Fact]
    public void Constructor_NullRaw_Allowed()
    {
        var parsed = new Parsed<DateOnly>(new DateOnly(2024, 6, 15), null);
        Assert.Null(parsed.Raw);
    }
}
