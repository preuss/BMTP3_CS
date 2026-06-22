using BMTP3.Core4.Engine.TimeStamp.Parsers;

namespace BMTP3.Core4.Tests.Engine.TimeStamp.Parsers;

public class TimestampResolutionEvaluatorTests
{
    // ---------------------------------------------------------------
    // DetermineEffectiveResolution
    // ---------------------------------------------------------------

    [Theory]
    [InlineData(1L, TimestampResolution.Seconds)]
    [InlineData(999_999_999_999L, TimestampResolution.Seconds)]       // 12 digits — below MilliThreshold
    [InlineData(1_000_000_000_000L, TimestampResolution.Milliseconds)] // 13 digits — at MilliThreshold
    [InlineData(999_999_999_999_999L, TimestampResolution.Milliseconds)] // 15 digits — below MicroThreshold
    [InlineData(1_000_000_000_000_000L, TimestampResolution.Microseconds)] // 16 digits — at MicroThreshold
    [InlineData(999_999_999_999_999_999L, TimestampResolution.Microseconds)] // 18 digits — below NanoThreshold
    [InlineData(1_000_000_000_000_000_000L, TimestampResolution.Nanoseconds)] // 19 digits — at NanoThreshold
    [InlineData(long.MaxValue, TimestampResolution.Nanoseconds)]
    [InlineData(0L, TimestampResolution.Seconds)]                     // zero — lowest resolution
    public void DetermineEffectiveResolution_ByMagnitude_ReturnsExpected(long value, TimestampResolution expected)
    {
        var result = TimestampResolutionEvaluator.DetermineEffectiveResolution(value, TimestampResolution.Seconds);
        Assert.Equal(expected, result);
    }

	[Theory]
	[InlineData(1L)]
	[InlineData(1_000_000_000_000L)]
	[InlineData(1_000_000_000_000_000L)]
	[InlineData(1_000_000_000_000_000_000L)]
	public void DetermineEffectiveResolution_NegativeValues_ReturnsSameAsPositive(long value)
	{
		var posResult = TimestampResolutionEvaluator.DetermineEffectiveResolution(value, TimestampResolution.Seconds);
		var negResult = TimestampResolutionEvaluator.DetermineEffectiveResolution(-value, TimestampResolution.Seconds);
		Assert.Equal(posResult, negResult);
	}

    [Theory]
    [InlineData(100L, TimestampResolution.Ticks100Ns)]
    [InlineData(3_155_378_975_999_999_999L, TimestampResolution.Ticks100Ns)] // max valid
    public void DetermineEffectiveResolution_Ticks100NsExpected_BypassesMagnitude(long value, TimestampResolution expected)
    {
        var result = TimestampResolutionEvaluator.DetermineEffectiveResolution(value, TimestampResolution.Ticks100Ns);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void DetermineEffectiveResolution_Ticks100Ns_ExceedsMax_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TimestampResolutionEvaluator.DetermineEffectiveResolution(
                3_155_378_976_000_000_000L, TimestampResolution.Ticks100Ns));
    }

    [Fact]
    public void DetermineEffectiveResolution_Ticks100Ns_NegativeExceedsMax_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TimestampResolutionEvaluator.DetermineEffectiveResolution(
                -3_155_378_976_000_000_000L, TimestampResolution.Ticks100Ns));
    }

    // ---------------------------------------------------------------
    // ScaleToResolution
    // ---------------------------------------------------------------

    [Fact]
    public void ScaleToResolution_SameResolution_ReturnsIdentity()
    {
        var result = TimestampResolutionEvaluator.ScaleToResolution(
            1000L, TimestampResolution.Seconds, TimestampResolution.Seconds);
        Assert.Equal(1000L, result);
    }

    [Fact]
    public void ScaleToResolution_SecondsToMilliseconds()
    {
        var result = TimestampResolutionEvaluator.ScaleToResolution(
            1L, TimestampResolution.Seconds, TimestampResolution.Milliseconds);
        Assert.Equal(1000L, result);
    }

    [Fact]
    public void ScaleToResolution_MillisecondsToSeconds()
    {
        var result = TimestampResolutionEvaluator.ScaleToResolution(
            1000L, TimestampResolution.Milliseconds, TimestampResolution.Seconds);
        Assert.Equal(1L, result);
    }

    [Fact]
    public void ScaleToResolution_SecondsToNanoseconds()
    {
        var result = TimestampResolutionEvaluator.ScaleToResolution(
            1L, TimestampResolution.Seconds, TimestampResolution.Nanoseconds);
        Assert.Equal(1_000_000_000L, result);
    }

    [Fact]
    public void ScaleToResolution_NanosecondsToSeconds()
    {
        var result = TimestampResolutionEvaluator.ScaleToResolution(
            1_000_000_000L, TimestampResolution.Nanoseconds, TimestampResolution.Seconds);
        Assert.Equal(1L, result);
    }

    [Fact]
    public void ScaleToResolution_MillisecondsToMicroseconds()
    {
        var result = TimestampResolutionEvaluator.ScaleToResolution(
            1L, TimestampResolution.Milliseconds, TimestampResolution.Microseconds);
        Assert.Equal(1000L, result);
    }

    [Fact]
    public void ScaleToResolution_MicrosecondsToMilliseconds()
    {
        var result = TimestampResolutionEvaluator.ScaleToResolution(
            1000L, TimestampResolution.Microseconds, TimestampResolution.Milliseconds);
        Assert.Equal(1L, result);
    }

    [Fact]
    public void ScaleToResolution_LargeValue_Overflows()
    {
        Assert.Throws<OverflowException>(() =>
            TimestampResolutionEvaluator.ScaleToResolution(
                long.MaxValue, TimestampResolution.Seconds, TimestampResolution.Nanoseconds));
    }

    [Fact]
    public void ScaleToResolution_UnsupportedFromResolution_Throws()
    {
        var unsupported = (TimestampResolution)999;
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TimestampResolutionEvaluator.ScaleToResolution(1L, unsupported, TimestampResolution.Seconds));
    }

    [Fact]
    public void ScaleToResolution_UnsupportedToResolution_Throws()
    {
        var unsupported = (TimestampResolution)999;
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TimestampResolutionEvaluator.ScaleToResolution(1L, TimestampResolution.Seconds, unsupported));
    }
}
