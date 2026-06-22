using BMTP3.Core4.Engine.TimeStamp.Candidates;

namespace BMTP3.Core4.Tests.Engine.TimeStamp.Candidates;

public class TimestampCandidateTests
{
    private static readonly TimestampSources SampleSources = new() { DateTime = "2024-06-15T14:30:00" };
    private static readonly DateOnly SampleDate = new(2024, 6, 15);
    private static readonly TimeOnly SampleTime = new(14, 30, 0);

    [Fact]
    public void Constructor_SubSecondsOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TimestampCandidate(TimestampSourceType.Exif, TimestampRole.Created, SampleSources,
                SampleDate, ChronoDateResolution.FullDate, SampleTime, -1, null));
        Assert.Contains("SubSeconds", ex.Message);

        var ex2 = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TimestampCandidate(TimestampSourceType.Exif, TimestampRole.Created, SampleSources,
                SampleDate, ChronoDateResolution.FullDate, SampleTime, 1_000_000_000, null));
        Assert.Contains("SubSeconds", ex2.Message);
    }

    [Fact]
    public void Constructor_DateWithoutDateResolution_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new TimestampCandidate(TimestampSourceType.Exif, TimestampRole.Created, SampleSources,
                SampleDate, null, null, null, null));
    }

    [Fact]
    public void Constructor_DateResolutionWithoutDate_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new TimestampCandidate(TimestampSourceType.Exif, TimestampRole.Created, SampleSources,
                null, ChronoDateResolution.FullDate, null, null, null));
    }

    [Fact]
    public void Constructor_ValidMinimal_CreatesInstance()
    {
        var c = new TimestampCandidate(TimestampSourceType.FileSystem, TimestampRole.Created,
            TimestampSources.Empty, null, null, null, null, null);
        Assert.Equal(TimestampSourceType.FileSystem, c.SourceType);
        Assert.Equal(TimestampRole.Created, c.Role);
        Assert.Null(c.Date);
        Assert.Null(c.DateResolution);
        Assert.Null(c.Time);
        Assert.Null(c.SubSeconds);
        Assert.Null(c.Offset);
        Assert.True(c.IsEmpty);
    }

    [Fact]
    public void Constructor_WithAllValues_SetsProperties()
    {
        var c = new TimestampCandidate(TimestampSourceType.Exif, TimestampRole.Digitized, SampleSources,
            SampleDate, ChronoDateResolution.FullDate, SampleTime, 500_000_000L, TimeSpan.FromHours(2));
        Assert.Equal(TimestampSourceType.Exif, c.SourceType);
        Assert.Equal(TimestampRole.Digitized, c.Role);
        Assert.Equal(SampleDate, c.Date);
        Assert.Equal(ChronoDateResolution.FullDate, c.DateResolution);
        Assert.Equal(SampleTime, c.Time);
        Assert.Equal(500_000_000L, c.SubSeconds);
        Assert.Equal(TimeSpan.FromHours(2), c.Offset);
        Assert.False(c.IsEmpty);
    }

    [Fact]
    public void IsEmpty_NoValues_ReturnsTrue()
    {
        var c = new TimestampCandidate(TimestampSourceType.FileSystem, TimestampRole.Unknown,
            TimestampSources.Empty, null, null, null, null, null);
        Assert.True(c.IsEmpty);
    }

    [Fact]
    public void IsEmpty_HasDate_ReturnsFalse()
    {
        var c = new TimestampCandidate(TimestampSourceType.FileSystem, TimestampRole.Created,
            SampleSources, SampleDate, ChronoDateResolution.FullDate, null, null, null);
        Assert.False(c.IsEmpty);
    }

    [Fact]
    public void EnsureValidComposition_SubSecondsWithoutTime_Throws()
    {
        var c = new TimestampCandidate(TimestampSourceType.Exif, TimestampRole.Created, SampleSources,
            null, null, null, 100L, null);
        Assert.Throws<InvalidOperationException>(() => c.EnsureValidComposition());
    }

    [Fact]
    public void EnsureValidComposition_OffsetWithoutDateAndTime_Throws()
    {
        var c = new TimestampCandidate(TimestampSourceType.Exif, TimestampRole.Created, SampleSources,
            null, null, null, null, TimeSpan.FromHours(1));
        Assert.Throws<InvalidOperationException>(() => c.EnsureValidComposition());
    }

    [Fact]
    public void EnsureValidComposition_TimeWithFractionalSeconds_Throws()
    {
        var timeWithFractions = new TimeOnly(14, 30, 0).Add(TimeSpan.FromTicks(1));
        var c = new TimestampCandidate(TimestampSourceType.Exif, TimestampRole.Created, SampleSources,
            SampleDate, ChronoDateResolution.FullDate, timeWithFractions, null, null);
        Assert.Throws<InvalidOperationException>(() => c.EnsureValidComposition());
    }

    [Fact]
    public void EnsureValidComposition_ValidFull_DoesNotThrow()
    {
        var c = new TimestampCandidate(TimestampSourceType.Exif, TimestampRole.Created, SampleSources,
            SampleDate, ChronoDateResolution.FullDate, SampleTime, 500_000_000L, TimeSpan.FromHours(2));
        c.EnsureValidComposition();
    }

    [Fact]
    public void IsValidComposition_Invalid_ReturnsFalse()
    {
        var c = new TimestampCandidate(TimestampSourceType.Exif, TimestampRole.Created, SampleSources,
            null, null, null, 100L, null);
        Assert.False(c.IsValidComposition());
    }

    [Fact]
    public void IsValidComposition_Valid_ReturnsTrue()
    {
        var c = new TimestampCandidate(TimestampSourceType.Exif, TimestampRole.Created, SampleSources,
            SampleDate, ChronoDateResolution.FullDate, SampleTime, null, null);
        Assert.True(c.IsValidComposition());
    }

    [Fact]
    public void TryToDateTime_MissingDate_ReturnsFalse()
    {
        var c = new TimestampCandidate(TimestampSourceType.Exif, TimestampRole.Created, SampleSources,
            null, null, SampleTime, null, null);
        Assert.False(c.TryToDateTime(out _));
    }

    [Fact]
    public void TryToDateTime_MissingTime_ReturnsFalse()
    {
        var c = new TimestampCandidate(TimestampSourceType.Exif, TimestampRole.Created, SampleSources,
            SampleDate, ChronoDateResolution.FullDate, null, null, null);
        Assert.False(c.TryToDateTime(out _));
    }

    [Fact]
    public void TryToDateTime_Valid_ReturnsTrue()
    {
        var c = new TimestampCandidate(TimestampSourceType.Exif, TimestampRole.Created, SampleSources,
            SampleDate, ChronoDateResolution.FullDate, SampleTime, null, null);
        Assert.True(c.TryToDateTime(out DateTime result));
        Assert.Equal(new DateTime(2024, 6, 15, 14, 30, 0), result);
        Assert.Equal(DateTimeKind.Unspecified, result.Kind);
    }

    [Fact]
    public void TryToDateTime_WithSubSeconds_IncludesTicks()
    {
        var c = new TimestampCandidate(TimestampSourceType.Exif, TimestampRole.Created, SampleSources,
            SampleDate, ChronoDateResolution.FullDate, SampleTime, 500_000_000L, null);
        Assert.True(c.TryToDateTime(out DateTime result));
        Assert.Equal(500_000_000L / 100, result.Ticks % TimeSpan.TicksPerSecond);
    }

    [Fact]
    public void TryToDateTimeOffset_MissingDate_ReturnsFalse()
    {
        var c = new TimestampCandidate(TimestampSourceType.Exif, TimestampRole.Created, SampleSources,
            null, null, SampleTime, null, TimeSpan.Zero);
        Assert.False(c.TryToDateTimeOffset(out _));
    }

    [Fact]
    public void TryToDateTimeOffset_MissingTime_ReturnsFalse()
    {
        var c = new TimestampCandidate(TimestampSourceType.Exif, TimestampRole.Created, SampleSources,
            SampleDate, ChronoDateResolution.FullDate, null, null, TimeSpan.Zero);
        Assert.False(c.TryToDateTimeOffset(out _));
    }

    [Fact]
    public void TryToDateTimeOffset_MissingOffset_ReturnsFalse()
    {
        var c = new TimestampCandidate(TimestampSourceType.Exif, TimestampRole.Created, SampleSources,
            SampleDate, ChronoDateResolution.FullDate, SampleTime, null, null);
        Assert.False(c.TryToDateTimeOffset(out _));
    }

    [Fact]
    public void TryToDateTimeOffset_Valid_ReturnsTrue()
    {
        var offset = TimeSpan.FromHours(2);
        var c = new TimestampCandidate(TimestampSourceType.Exif, TimestampRole.Created, SampleSources,
            SampleDate, ChronoDateResolution.FullDate, SampleTime, null, offset);
        Assert.True(c.TryToDateTimeOffset(out DateTimeOffset result));
        Assert.Equal(new DateTimeOffset(2024, 6, 15, 14, 30, 0, offset), result);
    }

    [Fact]
    public void ToString_Empty_ReturnsEmptyTimestamp()
    {
        var c = new TimestampCandidate(TimestampSourceType.FileSystem, TimestampRole.Unknown,
            TimestampSources.Empty, null, null, null, null, null);
        Assert.Equal("<empty timestamp>", c.ToString());
    }

    [Fact]
    public void ToString_IllegalComposition_ReturnsIllegalTimestamp()
    {
        var c = new TimestampCandidate(TimestampSourceType.Exif, TimestampRole.Created, SampleSources,
            null, null, null, 100L, null);
        Assert.Equal("<illegal timestamp>", c.ToString());
    }

    [Fact]
    public void ToString_Default_ReturnsFormatted()
    {
        var c = new TimestampCandidate(TimestampSourceType.Exif, TimestampRole.Created, SampleSources,
            SampleDate, ChronoDateResolution.FullDate, SampleTime, null, TimeSpan.Zero);
        var str = c.ToString();
        Assert.Contains("2024", str);
        Assert.Contains("06", str);
        Assert.Contains("15", str);
        Assert.Contains("14", str);
        Assert.Contains("30", str);
        Assert.Contains("00", str);
    }

    [Fact]
    public void ToString_WithIsoFormat_ProducesCorrectPattern()
    {
        var c = new TimestampCandidate(TimestampSourceType.Exif, TimestampRole.Created, SampleSources,
            SampleDate, ChronoDateResolution.FullDate, SampleTime, null, TimeSpan.Zero);
        var str = c.ToString(TimestampFormatStyle.Iso8601_DotFraction);
        Assert.Equal("2024-06-15T14:30:00Z", str);
    }

    [Fact]
    public void ToString_WithCompactFormat_ProducesCorrectPattern()
    {
        var c = new TimestampCandidate(TimestampSourceType.Exif, TimestampRole.Created, SampleSources,
            SampleDate, ChronoDateResolution.FullDate, SampleTime, null, TimeSpan.Zero);
        var str = c.ToString(TimestampFormatStyle.Compact_DotFraction);
        Assert.Equal("20240615T143000Z", str);
    }

    [Fact]
    public void ToString_WithSubSeconds_IncludesFraction()
    {
        var c = new TimestampCandidate(TimestampSourceType.Exif, TimestampRole.Created, SampleSources,
            SampleDate, ChronoDateResolution.FullDate, SampleTime, 500_000_000L, TimeSpan.Zero);
        var str = c.ToString(TimestampFormatStyle.Iso8601_DotFraction);
        Assert.Equal("2024-06-15T14:30:00.5Z", str);
    }

    [Fact]
    public void ToString_IFormattable_NullFormat_UsesDefault()
    {
        var c = new TimestampCandidate(TimestampSourceType.Exif, TimestampRole.Created, SampleSources,
            SampleDate, ChronoDateResolution.FullDate, SampleTime, null, TimeSpan.Zero);
        var str = ((IFormattable)c).ToString(null, null);
        Assert.Equal("2024-06-15T14:30:00Z", str);
    }

    [Fact]
    public void ToString_IFormattable_WithFormat_Delegates()
    {
        var c = new TimestampCandidate(TimestampSourceType.Exif, TimestampRole.Created, SampleSources,
            SampleDate, ChronoDateResolution.FullDate, SampleTime, null, TimeSpan.Zero);
        var str = ((IFormattable)c).ToString("compact", null);
        Assert.Equal("20240615T143000Z", str);
    }

    [Fact]
    public void ToDebugString_IncludesAllFields()
    {
        var c = new TimestampCandidate(TimestampSourceType.Exif, TimestampRole.Created, SampleSources,
            SampleDate, ChronoDateResolution.FullDate, SampleTime, 500_000_000L, TimeSpan.FromHours(2));
        var str = c.ToDebugString();
        Assert.Contains("SourceType : Exif", str);
        Assert.Contains("Role       : Created", str);
        Assert.Contains("Date       :", str);
        Assert.Contains("2024", str);
        Assert.Contains("Time       :", str);
        Assert.Contains("14", str);
        Assert.Contains("30", str);
        Assert.Contains("SubSeconds : 500000000", str);
        Assert.Contains("Offset     : 02:00:00", str);
    }

    [Fact]
    public void ToDebugString_EmptyCandidate()
    {
        var c = new TimestampCandidate(TimestampSourceType.FileSystem, TimestampRole.Unknown,
            TimestampSources.Empty, null, null, null, null, null);
        var str = c.ToDebugString();
        Assert.Contains("∅", str);
    }

    [Fact]
    public void Offset_NonZero_ProducesOffsetString()
    {
        var offset = TimeSpan.FromHours(2);
        var c = new TimestampCandidate(TimestampSourceType.Exif, TimestampRole.Created, SampleSources,
            SampleDate, ChronoDateResolution.FullDate, SampleTime, null, offset);
        var str = c.ToString(TimestampFormatStyle.Iso8601_DotFraction);
        Assert.Contains("+02:00", str);
    }

    [Fact]
    public void Offset_Negative_ProducesNegativeOffsetString()
    {
        var offset = TimeSpan.FromHours(-5);
        var c = new TimestampCandidate(TimestampSourceType.Exif, TimestampRole.Created, SampleSources,
            SampleDate, ChronoDateResolution.FullDate, SampleTime, null, offset);
        var str = c.ToString(TimestampFormatStyle.Iso8601_DotFraction);
        Assert.Contains("-05:00", str);
    }

    [Fact]
    public void NoOffset_ProducesNoOffsetInOutput()
    {
        var c = new TimestampCandidate(TimestampSourceType.Exif, TimestampRole.Created, SampleSources,
            SampleDate, ChronoDateResolution.FullDate, SampleTime, null, null);
        var str = c.ToString(TimestampFormatStyle.Iso8601_DotFraction);
        Assert.DoesNotContain("Z", str);
        Assert.DoesNotContain("+", str);
        Assert.DoesNotContain("-", str[10..]);
    }

    [Fact]
    public void DateOnly_FormatsWithoutTime()
    {
        var c = new TimestampCandidate(TimestampSourceType.Exif, TimestampRole.Created, SampleSources,
            SampleDate, ChronoDateResolution.FullDate, null, null, null);
        var str = c.ToString(TimestampFormatStyle.Iso8601_DotFraction);
        Assert.Equal("2024-06-15", str);
    }

    [Fact]
    public void TimeOnly_FormatsWithoutDate()
    {
        var c = new TimestampCandidate(TimestampSourceType.Exif, TimestampRole.Created, SampleSources,
            null, null, SampleTime, null, null);
        var str = c.ToString(TimestampFormatStyle.Iso8601_DotFraction);
        Assert.Equal("14:30:00", str);
    }
}
