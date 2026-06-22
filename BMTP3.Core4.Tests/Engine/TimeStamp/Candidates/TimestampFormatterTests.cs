using BMTP3.Core4.Engine.TimeStamp.Candidates;
using BMTP3.Core4.Engine.TimeStamp.Candidates.Parsing;

namespace BMTP3.Core4.Tests.Engine.TimeStamp.Candidates;

public class TimestampFormatterTests
{
    private static readonly ITimestampFormatter Formatter = new TimestampFormatter();
    private static readonly DateOnly SampleDate = new(2024, 6, 15);
    private static readonly TimeOnly SampleTime = new(14, 30, 0);
    private static readonly TimestampSources Sources = new() { DateTime = "2024-06-15T14:30:00" };

    private static TimestampCandidate MakeCandidate(DateOnly? date = null, TimeOnly? time = null,
        long? subSeconds = null, TimeSpan? offset = null,
        ChronoDateResolution? resolution = null)
    {
        if (resolution is null && date.HasValue)
            resolution = ChronoDateResolution.FullDate;
        return new TimestampCandidate(TimestampSourceType.Exif, TimestampRole.Created, Sources,
            date, resolution, time, subSeconds, offset);
    }

    // ── Iso8601_DotFraction ──

    [Fact]
    public void Iso8601_DotFraction_FullDateTimeUtc()
    {
        var c = MakeCandidate(SampleDate, SampleTime, offset: TimeSpan.Zero);
        Assert.Equal("2024-06-15T14:30:00Z", Formatter.Format(c, TimestampFormatStyle.Iso8601_DotFraction));
    }

    [Fact]
    public void Iso8601_DotFraction_FullDateTimeWithOffset()
    {
        var c = MakeCandidate(SampleDate, SampleTime, offset: TimeSpan.FromHours(2));
        Assert.Equal("2024-06-15T14:30:00+02:00:00", Formatter.Format(c, TimestampFormatStyle.Iso8601_DotFraction));
    }

    [Fact]
    public void Iso8601_DotFraction_FullDateTimeNoOffset()
    {
        var c = MakeCandidate(SampleDate, SampleTime);
        Assert.Equal("2024-06-15T14:30:00", Formatter.Format(c, TimestampFormatStyle.Iso8601_DotFraction));
    }

    [Fact]
    public void Iso8601_DotFraction_WithSubSeconds()
    {
        var c = MakeCandidate(SampleDate, SampleTime, subSeconds: 500_000_000L, offset: TimeSpan.Zero);
        Assert.Equal("2024-06-15T14:30:00.5Z", Formatter.Format(c, TimestampFormatStyle.Iso8601_DotFraction));
    }

    [Fact]
    public void Iso8601_DotFraction_WithSubSecondsTruncated()
    {
        var c = MakeCandidate(SampleDate, SampleTime, subSeconds: 123_456_789L, offset: TimeSpan.Zero);
        Assert.Equal("2024-06-15T14:30:00.123456789Z", Formatter.Format(c, TimestampFormatStyle.Iso8601_DotFraction));
    }

    [Fact]
    public void Iso8601_DotFraction_DateOnly()
    {
        var c = MakeCandidate(SampleDate);
        Assert.Equal("2024-06-15", Formatter.Format(c, TimestampFormatStyle.Iso8601_DotFraction));
    }

    [Fact]
    public void Iso8601_DotFraction_TimeOnly()
    {
        var c = MakeCandidate(time: SampleTime);
        Assert.Equal("14:30:00", Formatter.Format(c, TimestampFormatStyle.Iso8601_DotFraction));
    }

    // ── Iso8601_CommaFraction ──

    [Fact]
    public void Iso8601_CommaFraction_FullDateTimeUtc()
    {
        var c = MakeCandidate(SampleDate, SampleTime, offset: TimeSpan.Zero);
        Assert.Equal("2024-06-15T14:30:00Z", Formatter.Format(c, TimestampFormatStyle.Iso8601_CommaFraction));
    }

    [Fact]
    public void Iso8601_CommaFraction_WithSubSeconds()
    {
        var c = MakeCandidate(SampleDate, SampleTime, subSeconds: 500_000_000L, offset: TimeSpan.Zero);
        Assert.Equal("2024-06-15T14:30:00,5Z", Formatter.Format(c, TimestampFormatStyle.Iso8601_CommaFraction));
    }

    // ── Compact_DotFraction ──

    [Fact]
    public void Compact_DotFraction_FullDateTimeUtc()
    {
        var c = MakeCandidate(SampleDate, SampleTime, offset: TimeSpan.Zero);
        Assert.Equal("20240615T143000Z", Formatter.Format(c, TimestampFormatStyle.Compact_DotFraction));
    }

    [Fact]
    public void Compact_DotFraction_WithOffset()
    {
        var c = MakeCandidate(SampleDate, SampleTime, offset: TimeSpan.FromHours(2));
        Assert.Equal("20240615T143000+020000", Formatter.Format(c, TimestampFormatStyle.Compact_DotFraction));
    }

    [Fact]
    public void Compact_DotFraction_WithSubSeconds()
    {
        var c = MakeCandidate(SampleDate, SampleTime, subSeconds: 500_000_000L, offset: TimeSpan.Zero);
        Assert.Equal("20240615T143000.5Z", Formatter.Format(c, TimestampFormatStyle.Compact_DotFraction));
    }

    // ── Compact_CommaFraction ──

    [Fact]
    public void Compact_CommaFraction_FullDateTimeUtc()
    {
        var c = MakeCandidate(SampleDate, SampleTime, offset: TimeSpan.Zero);
        Assert.Equal("20240615T143000Z", Formatter.Format(c, TimestampFormatStyle.Compact_CommaFraction));
    }

    [Fact]
    public void Compact_CommaFraction_WithSubSeconds()
    {
        var c = MakeCandidate(SampleDate, SampleTime, subSeconds: 500_000_000L, offset: TimeSpan.Zero);
        Assert.Equal("20240615T143000,5Z", Formatter.Format(c, TimestampFormatStyle.Compact_CommaFraction));
    }

    // ── Compact_Underscore variants ──

    [Fact]
    public void Compact_Underscore_DotFraction_UsesUnderscore()
    {
        var c = MakeCandidate(SampleDate, SampleTime, offset: TimeSpan.Zero);
        Assert.Equal("20240615_143000Z", Formatter.Format(c, TimestampFormatStyle.Compact_Underscore_DotFraction));
    }

    [Fact]
    public void Compact_Underscore_CommaFraction_UsesUnderscoreAndComma()
    {
        var c = MakeCandidate(SampleDate, SampleTime, subSeconds: 500_000_000L, offset: TimeSpan.Zero);
        Assert.Equal("20240615_143000,5Z", Formatter.Format(c, TimestampFormatStyle.Compact_Underscore_CommaFraction));
    }

    // ── OffsetDuration variants ──

    [Fact]
    public void OffsetDuration_DotFraction_DurationOffset()
    {
        var c = MakeCandidate(SampleDate, SampleTime, offset: TimeSpan.FromHours(2));
        Assert.Equal("2024-06-15T14:30:00+2H",
            Formatter.Format(c, TimestampFormatStyle.Iso8601_DotFraction_OffsetDuration));
    }

    [Fact]
    public void OffsetDuration_DotFraction_OffsetWithMinutes()
    {
        var c = MakeCandidate(SampleDate, SampleTime, offset: new TimeSpan(2, 30, 0));
        Assert.Equal("2024-06-15T14:30:00+2H30M",
            Formatter.Format(c, TimestampFormatStyle.Iso8601_DotFraction_OffsetDuration));
    }

    [Fact]
    public void OffsetDuration_DotFraction_OffsetWithSeconds()
    {
        var c = MakeCandidate(SampleDate, SampleTime, offset: new TimeSpan(2, 30, 15));
        Assert.Equal("2024-06-15T14:30:00+2H30M15S",
            Formatter.Format(c, TimestampFormatStyle.Iso8601_DotFraction_OffsetDuration));
    }

    [Fact]
    public void OffsetDuration_CommaFraction_WithFractions()
    {
        var c = MakeCandidate(SampleDate, SampleTime, subSeconds: 500_000_000L, offset: new TimeSpan(2, 30, 15));
        Assert.Equal("2024-06-15T14:30:00,5+2H30M15S",
            Formatter.Format(c, TimestampFormatStyle.Iso8601_CommaFraction_OffsetDuration));
    }

    // ── DotTime variants ──

    [Fact]
    public void DotTime_DotFraction_UsesDotInTime()
    {
        var c = MakeCandidate(SampleDate, SampleTime, offset: TimeSpan.Zero);
        Assert.Equal("2024-06-15T14.30.00Z", Formatter.Format(c, TimestampFormatStyle.DotTime_DotFraction));
    }

    [Fact]
    public void DotTime_DotFraction_WithSubSeconds()
    {
        var c = MakeCandidate(SampleDate, SampleTime, subSeconds: 500_000_000L, offset: TimeSpan.Zero);
        Assert.Equal("2024-06-15T14.30.00.5Z", Formatter.Format(c, TimestampFormatStyle.DotTime_DotFraction));
    }

    [Fact]
    public void DotTime_DotFraction_DottedOffset()
    {
        var c = MakeCandidate(SampleDate, SampleTime, offset: TimeSpan.FromHours(2));
        Assert.Equal("2024-06-15T14.30.00+02.00.00", Formatter.Format(c, TimestampFormatStyle.DotTime_DotFraction));
    }

    // ── DotDateTime variants ──

    [Fact]
    public void DotDateTime_Underscore_DotFraction()
    {
        var c = MakeCandidate(SampleDate, SampleTime, offset: TimeSpan.Zero);
        Assert.Equal("2024.06.15_14.30.00Z", Formatter.Format(c, TimestampFormatStyle.DotDateTime_Underscore_DotFraction));
    }

    [Fact]
    public void DotDateTime_DotFraction_UsesT()
    {
        var c = MakeCandidate(SampleDate, SampleTime, offset: TimeSpan.Zero);
        Assert.Equal("2024.06.15T14.30.00Z", Formatter.Format(c, TimestampFormatStyle.DotDateTime_DotFraction));
    }

    // ── Danish variants ──

    [Fact]
    public void DanishSeparator_CommaFraction_UsesSlash()
    {
        var c = MakeCandidate(SampleDate, SampleTime, offset: TimeSpan.Zero);
        Assert.Equal("2024-06/15T14:30:00Z", Formatter.Format(c, TimestampFormatStyle.DanishSeparator_CommaFraction));
    }

    [Fact]
    public void DanishSeparator_Underscore_CommaFraction()
    {
        var c = MakeCandidate(SampleDate, SampleTime, offset: TimeSpan.Zero);
        Assert.Equal("2024-06/15_14:30:00Z", Formatter.Format(c, TimestampFormatStyle.DanishSeparator_Underscore_CommaFraction));
    }

    // ── Edge cases ──

    [Fact]
    public void Format_EmptyCandidate_ReturnsEmptyString()
    {
        var c = new TimestampCandidate(TimestampSourceType.FileSystem, TimestampRole.Unknown,
            TimestampSources.Empty, null, null, null, null, null);
        Assert.Equal("", Formatter.Format(c, TimestampFormatStyle.Iso8601_DotFraction));
    }

    [Fact]
    public void Format_IllegalComposition_ThrowsInvalidOperationException()
    {
        var c = new TimestampCandidate(TimestampSourceType.Exif, TimestampRole.Created, Sources,
            null, null, null, 100L, null);
        Assert.Throws<InvalidOperationException>(() =>
            Formatter.Format(c, TimestampFormatStyle.Iso8601_DotFraction));
    }

    [Fact]
    public void Format_NegativeOffset_ProducesSign()
    {
        var c = MakeCandidate(SampleDate, SampleTime, offset: TimeSpan.FromHours(-5));
        Assert.Contains("-05:00", Formatter.Format(c, TimestampFormatStyle.Iso8601_DotFraction));
    }

    [Fact]
    public void Format_ZeroSubSeconds_Omitted()
    {
        var c = MakeCandidate(SampleDate, SampleTime, subSeconds: 0, offset: TimeSpan.Zero);
        Assert.Equal("2024-06-15T14:30:00Z", Formatter.Format(c, TimestampFormatStyle.Iso8601_DotFraction));
    }

    [Fact]
    public void Format_InvalidCandidate_SubSecondsWithoutTime_Throws()
    {
        var c = new TimestampCandidate(TimestampSourceType.Exif, TimestampRole.Created, Sources,
            null, null, null, 100L, null);
        Assert.Throws<InvalidOperationException>(() =>
            Formatter.Format(c, TimestampFormatStyle.Iso8601_DotFraction));
    }

    // ── Format with descriptor directly ──

    [Fact]
    public void Format_WithDescriptor_Direct()
    {
        var c = MakeCandidate(SampleDate, SampleTime, offset: TimeSpan.Zero);
        var descriptor = TimestampFormatDescriptor.Describe(TimestampFormatStyle.Iso8601_DotFraction);
        Assert.Equal("2024-06-15T14:30:00Z", Formatter.Format(c, descriptor));
    }
}
