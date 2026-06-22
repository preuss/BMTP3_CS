using BMTP3.Core4.Engine.TimeStamp.Candidates;
using BMTP3.Core4.Engine.TimeStamp.Parsers;

namespace BMTP3.Core4.Tests.Engine.TimeStamp.Candidates;

public class TimestampCandidateFactoryTests
{
    private static readonly DateOnly SampleDate = new(2024, 6, 15);
    private static readonly TimeOnly SampleTime = new(14, 30, 0);

    // ── OffsetToTimeSpan ──

    [Fact]
    public void OffsetToTimeSpan_Utc_ReturnsZero()
    {
        var dt = new DateTime(2024, 6, 15, 14, 30, 0, DateTimeKind.Utc);
        var result = TimestampCandidateFactory.OffsetToTimeSpan(dt);
        Assert.Equal(TimeSpan.Zero, result);
    }

    [Fact]
    public void OffsetToTimeSpan_Unspecified_ReturnsNull()
    {
        var dt = new DateTime(2024, 6, 15, 14, 30, 0, DateTimeKind.Unspecified);
        Assert.Null(TimestampCandidateFactory.OffsetToTimeSpan(dt));
    }

    [Fact]
    public void OffsetToTimeSpan_DateTimeOffset_ReturnsOffset()
    {
        var dto = new DateTimeOffset(2024, 6, 15, 14, 30, 0, TimeSpan.FromHours(2));
        Assert.Equal(TimeSpan.FromHours(2), TimestampCandidateFactory.OffsetToTimeSpan(dto));
    }

    // ── TrimToNanoseconds ──

    [Fact]
    public void TrimToNanoseconds_Null_ReturnsNull()
    {
        Assert.Null(TimestampCandidateFactory.TrimToNanoseconds((long?)null));
        Assert.Null(TimestampCandidateFactory.TrimToNanoseconds((TimeOnly?)null));
        Assert.Null(TimestampCandidateFactory.TrimToNanoseconds((DateTime?)null));
        Assert.Null(TimestampCandidateFactory.TrimToNanoseconds((DateTimeOffset?)null));
    }

    [Fact]
    public void TrimToNanoseconds_NoFractions_ReturnsNull()
    {
        var dt = new DateTime(2024, 6, 15, 14, 30, 0, DateTimeKind.Utc);
        Assert.Null(TimestampCandidateFactory.TrimToNanoseconds(dt));
    }

    [Fact]
    public void TrimToNanoseconds_WithFractions_ReturnsNanoseconds()
    {
        var dt = new DateTime(2024, 6, 15, 14, 30, 0, DateTimeKind.Utc).AddTicks(5000);
        var result = TimestampCandidateFactory.TrimToNanoseconds(dt);
        Assert.NotNull(result);
        Assert.Equal(5000L * 100, result.Value);
    }

    [Fact]
    public void TrimToNanoseconds_TimeOnlyWithFractions_ReturnsNanoseconds()
    {
        var time = new TimeOnly(14, 30, 0).Add(TimeSpan.FromTicks(1234));
        var result = TimestampCandidateFactory.TrimToNanoseconds(time);
        Assert.NotNull(result);
        Assert.Equal(1234L * 100, result.Value);
    }

    // ── FromDateTime ──

    [Fact]
    public void FromDateTime_Utc_SetsOffsetZero()
    {
        var dt = new DateTime(2024, 6, 15, 14, 30, 0, DateTimeKind.Utc);
        var c = TimestampCandidateFactory.FromDateTime(TimestampSourceType.Exif, TimestampRole.Created, dt);
        Assert.Equal(TimeSpan.Zero, c.Offset);
        Assert.Equal(SampleDate, c.Date);
        Assert.Equal(SampleTime, c.Time);
        Assert.Equal(ChronoDateResolution.FullDate, c.DateResolution);
    }

    [Fact]
    public void FromDateTime_Unspecified_SetsOffsetNull()
    {
        var dt = new DateTime(2024, 6, 15, 14, 30, 0, DateTimeKind.Unspecified);
        var c = TimestampCandidateFactory.FromDateTime(TimestampSourceType.Exif, TimestampRole.Created, dt);
        Assert.Null(c.Offset);
    }

    [Fact]
    public void FromDateTime_SourceTypeAndRole_AreSet()
    {
        var dt = new DateTime(2024, 6, 15, 14, 30, 0, DateTimeKind.Utc);
        var c = TimestampCandidateFactory.FromDateTime(TimestampSourceType.FileSystem, TimestampRole.Modified, dt);
        Assert.Equal(TimestampSourceType.FileSystem, c.SourceType);
        Assert.Equal(TimestampRole.Modified, c.Role);
    }

    // ── FromDateTimeOffset ──

    [Fact]
    public void FromDateTimeOffset_SetsOffset()
    {
        var dto = new DateTimeOffset(2024, 6, 15, 14, 30, 0, TimeSpan.FromHours(2));
        var c = TimestampCandidateFactory.FromDateTimeOffset(TimestampSourceType.Exif, TimestampRole.Created, dto);
        Assert.Equal(TimeSpan.FromHours(2), c.Offset);
        Assert.Equal(SampleDate, c.Date);
        Assert.Equal(SampleTime, c.Time);
    }

    // ── FromUtcDateAndTime ──

    [Fact]
    public void FromUtcDateAndTime_SetsOffsetZero()
    {
        var c = TimestampCandidateFactory.FromUtcDateAndTime(
            TimestampSourceType.Exif, TimestampRole.Created,
            "2024:06:15", "14:30:00",
            SampleDate, SampleTime);
        Assert.Equal(TimeSpan.Zero, c.Offset);
        Assert.Equal(SampleDate, c.Date);
        Assert.Equal(SampleTime, c.Time);
        Assert.Equal(ChronoDateResolution.FullDate, c.DateResolution);
    }

    [Fact]
    public void FromUtcDateAndTime_NullDateWithFullDateResolution_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            TimestampCandidateFactory.FromUtcDateAndTime(
                TimestampSourceType.Exif, TimestampRole.Created,
                null, null, null, null));
    }

    // ── ConvertTimestampUtcToDateTimeOffset ──

    [Theory]
    [InlineData(EpochType.Unix, 0L, 1970, 1, 1)]
    [InlineData(EpochType.MacLegacy, 0L, 1904, 1, 1)]
    [InlineData(EpochType.MacCocoa, 0L, 2001, 1, 1)]
    [InlineData(EpochType.DotNetTicks, 0L, 1, 1, 1)]
    [InlineData(EpochType.WindowsFileTime, 0L, 1601, 1, 1)]
    [InlineData(EpochType.GPS, 0L, 1980, 1, 6)]
    [InlineData(EpochType.NTP_Timestamp, 0L, 1900, 1, 1)]
    [InlineData(EpochType.FatDos, 0L, 1980, 1, 1)]
    public void ConvertTimestampUtcToDateTimeOffset_EpochZero(EpochType epoch, long value, int year, int month, int day)
    {
        var result = TimestampCandidateFactory.ConvertTimestampUtcToDateTimeOffset(value, epoch, TimestampResolution.Seconds);
        Assert.Equal(new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero), result);
    }

    [Theory]
    [InlineData(TimestampResolution.Seconds, 1L, 1)]
    [InlineData(TimestampResolution.Milliseconds, 1000L, 1)]
    [InlineData(TimestampResolution.Microseconds, 1_000_000L, 1)]
    [InlineData(TimestampResolution.Ticks100Ns, 10_000_000L, 1)]
    [InlineData(TimestampResolution.Nanoseconds, 1_000_000_000L, 1)]
    public void ConvertTimestampUtcToDateTimeOffset_Resolution_AddsCorrectOffset(TimestampResolution resolution, long value, int expectedSeconds)
    {
        var result = TimestampCandidateFactory.ConvertTimestampUtcToDateTimeOffset(value, EpochType.Unix, resolution);
        var expected = DateTimeOffset.UnixEpoch.AddSeconds(expectedSeconds);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ConvertTimestampUtcToDateTimeOffset_UnknownEpoch_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TimestampCandidateFactory.ConvertTimestampUtcToDateTimeOffset(0, (EpochType)999, TimestampResolution.Seconds));
    }

    [Fact]
    public void ConvertTimestampUtcToDateTimeOffset_UnknownResolution_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TimestampCandidateFactory.ConvertTimestampUtcToDateTimeOffset(0, EpochType.Unix, (TimestampResolution)999));
    }

    // ── FromRawTimestamp ──

    [Fact]
    public void FromRawTimestamp_CreatesCandidate()
    {
        var c = TimestampCandidateFactory.FromRawTimestamp(
            TimestampSourceType.Exif, TimestampRole.Created, "1700000000",
            1700000000L, EpochType.Unix, TimestampResolution.Seconds);
        Assert.Equal(TimestampSourceType.Exif, c.SourceType);
        Assert.Equal(TimestampRole.Created, c.Role);
        Assert.Equal("1700000000", c.Source.Timestamp);
        Assert.Equal(ChronoDateResolution.FullDate, c.DateResolution);
    }

    // ── FromRawDateTimeOffset ──

    [Fact]
    public void FromRawDateTimeOffset_Empty_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            TimestampCandidateFactory.FromRawDateTimeOffset(
                TimestampSourceType.Exif, TimestampRole.Created, "  ",
                DateTimeOffset.Now));
    }

    [Fact]
    public void FromRawDateTimeOffset_Valid_CreatesCandidate()
    {
        var dto = new DateTimeOffset(2024, 6, 15, 14, 30, 0, TimeSpan.FromHours(2));
        var c = TimestampCandidateFactory.FromRawDateTimeOffset(
            TimestampSourceType.Exif, TimestampRole.Created, "2024-06-15T14:30:00+02:00", dto);
        Assert.Equal(SampleDate, c.Date);
        Assert.Equal(SampleTime, c.Time);
        Assert.Equal(TimeSpan.FromHours(2), c.Offset);
    }

    // ── FromRawDateTime ──

    [Fact]
    public void FromRawDateTime_Empty_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            TimestampCandidateFactory.FromRawDateTime(
                TimestampSourceType.Exif, TimestampRole.Created, "  ",
                DateTime.Now));
    }

    [Fact]
    public void FromRawDateTime_Valid_CreatesCandidate()
    {
        var dt = new DateTime(2024, 6, 15, 14, 30, 0, DateTimeKind.Unspecified);
        var c = TimestampCandidateFactory.FromRawDateTime(
            TimestampSourceType.Exif, TimestampRole.Created, "2024-06-15 14:30:00", dt);
        Assert.Equal(SampleDate, c.Date);
        Assert.Equal(SampleTime, c.Time);
        Assert.Null(c.Offset);
    }

    // ── FromRawDate ──

    [Fact]
    public void FromRawDate_Empty_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            TimestampCandidateFactory.FromRawDate(
                TimestampSourceType.Exif, TimestampRole.Created, "  ",
                SampleDate, ChronoDateResolution.FullDate));
    }

    [Fact]
    public void FromRawDate_Valid_CreatesCandidate()
    {
        var c = TimestampCandidateFactory.FromRawDate(
            TimestampSourceType.Exif, TimestampRole.Created, "2024:06:15",
            SampleDate, ChronoDateResolution.FullDate);
        Assert.Equal(SampleDate, c.Date);
        Assert.Equal(ChronoDateResolution.FullDate, c.DateResolution);
        Assert.Null(c.Time);
        Assert.Null(c.SubSeconds);
        Assert.Null(c.Offset);
    }

    // ── FromRawTime ──

    [Fact]
    public void FromRawTime_Empty_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            TimestampCandidateFactory.FromRawTime(
                TimestampSourceType.Exif, TimestampRole.Created, "  ",
                SampleTime));
    }

    [Fact]
    public void FromRawTime_Valid_CreatesCandidate()
    {
        var c = TimestampCandidateFactory.FromRawTime(
            TimestampSourceType.Exif, TimestampRole.Created, "14:30:00",
            SampleTime);
        Assert.Equal(SampleTime, c.Time);
        Assert.Null(c.Date);
        Assert.Null(c.Offset);
    }

    [Fact]
    public void FromRawTime_StripsSubSeconds()
    {
        var timeWithFractions = new TimeOnly(14, 30, 0).Add(TimeSpan.FromTicks(5000));
        var c = TimestampCandidateFactory.FromRawTime(
            TimestampSourceType.Exif, TimestampRole.Created, "14:30:00.5",
            timeWithFractions);
        Assert.Equal(SampleTime, c.Time);
        Assert.NotNull(c.SubSeconds);
    }

    // ── FromRawOffset ──

    [Fact]
    public void FromRawOffset_Empty_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            TimestampCandidateFactory.FromRawOffset(
                TimestampSourceType.Exif, TimestampRole.Created, "  ",
                TimeSpan.FromHours(2)));
    }

    [Fact]
    public void FromRawOffset_Valid_CreatesCandidate()
    {
        var offset = TimeSpan.FromHours(2);
        var c = TimestampCandidateFactory.FromRawOffset(
            TimestampSourceType.Exif, TimestampRole.Created, "+02:00", offset);
        Assert.Equal(offset, c.Offset);
        Assert.Null(c.Date);
        Assert.Null(c.Time);
    }

    // ── FromRawSubSeconds ──

    [Fact]
    public void FromRawSubSeconds_Empty_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            TimestampCandidateFactory.FromRawSubSeconds(
                TimestampSourceType.Exif, TimestampRole.Created, "  ",
                500_000_000L));
    }

    [Fact]
    public void FromRawSubSeconds_Valid_CreatesCandidate()
    {
        var c = TimestampCandidateFactory.FromRawSubSeconds(
            TimestampSourceType.Exif, TimestampRole.Created, "500000000",
            500_000_000L);
        Assert.Equal(500_000_000L, c.SubSeconds);
        Assert.Null(c.Date);
        Assert.Null(c.Time);
    }

    // ── FromParts ──

    [Fact]
    public void FromParts_DateWithoutResolution_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            TimestampCandidateFactory.FromParts(
                TimestampSourceType.Exif, TimestampRole.Created,
                new TimestampSources { Date = "2024:06:15" },
                SampleDate, null, null, null, null));
    }

    [Fact]
    public void FromParts_DateWithoutSource_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            TimestampCandidateFactory.FromParts(
                TimestampSourceType.Exif, TimestampRole.Created,
                TimestampSources.Empty,
                SampleDate, ChronoDateResolution.FullDate, null, null, null));
    }

    [Fact]
    public void FromParts_TimeWithoutSource_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            TimestampCandidateFactory.FromParts(
                TimestampSourceType.Exif, TimestampRole.Created,
                new TimestampSources { Date = "2024:06:15" },
                SampleDate, ChronoDateResolution.FullDate, SampleTime, null, null));
    }

    [Fact]
    public void FromParts_SubSecondsWithoutSource_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            TimestampCandidateFactory.FromParts(
                TimestampSourceType.Exif, TimestampRole.Created,
                new TimestampSources { Date = "2024:06:15" },
                SampleDate, ChronoDateResolution.FullDate, null, 500L, null));
    }

    [Fact]
    public void FromParts_OffsetWithoutSource_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            TimestampCandidateFactory.FromParts(
                TimestampSourceType.Exif, TimestampRole.Created,
                new TimestampSources { Date = "2024:06:15", Time = "14:30:00" },
                SampleDate, ChronoDateResolution.FullDate, SampleTime, null, TimeSpan.FromHours(1)));
    }

    [Fact]
    public void FromParts_ValidWithDateTimeOffsetSource_Succeeds()
    {
        var sources = new TimestampSources { DateTimeOffset = "2024-06-15T14:30:00+02:00" };
        var c = TimestampCandidateFactory.FromParts(
            TimestampSourceType.Exif, TimestampRole.Created, sources,
            SampleDate, ChronoDateResolution.FullDate, SampleTime, 500_000_000L, TimeSpan.FromHours(2));
        Assert.Equal(SampleDate, c.Date);
        Assert.Equal(SampleTime, c.Time);
        Assert.Equal(500_000_000L, c.SubSeconds);
        Assert.Equal(TimeSpan.FromHours(2), c.Offset);
    }

    [Fact]
    public void FromParts_NullSources_NormalizedToEmpty()
    {
        Assert.Throws<ArgumentException>(() =>
            TimestampCandidateFactory.FromParts(
                TimestampSourceType.Exif, TimestampRole.Created,
                null, SampleDate, ChronoDateResolution.FullDate, null, null, null));
    }

    // ── FromRawDateWithTimeAndOffSetAndSubSec ──

    [Fact]
    public void FromRawDateWithTimeAndOffSetAndSubSec_AllEmpty_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            TimestampCandidateFactory.FromRawDateWithTimeAndOffSetAndSubSec(
                TimestampSourceType.Exif, TimestampRole.Created,
                "  ", null, null, null,
                SampleDate, ChronoDateResolution.FullDate, null, null, null));
    }

    [Fact]
    public void FromRawDateWithTimeAndOffSetAndSubSec_TimeWithoutRaw_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            TimestampCandidateFactory.FromRawDateWithTimeAndOffSetAndSubSec(
                TimestampSourceType.Exif, TimestampRole.Created,
                "2024:06:15", null, null, null,
                SampleDate, ChronoDateResolution.FullDate, SampleTime, null, null));
    }

    [Fact]
    public void FromRawDateWithTimeAndOffSetAndSubSec_OffsetWithoutRaw_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            TimestampCandidateFactory.FromRawDateWithTimeAndOffSetAndSubSec(
                TimestampSourceType.Exif, TimestampRole.Created,
                "2024:06:15", "14:30:00", null, null,
                SampleDate, ChronoDateResolution.FullDate, SampleTime, TimeSpan.FromHours(2), null));
    }

    [Fact]
    public void FromRawDateWithTimeAndOffSetAndSubSec_SubSecWithoutRaw_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            TimestampCandidateFactory.FromRawDateWithTimeAndOffSetAndSubSec(
                TimestampSourceType.Exif, TimestampRole.Created,
                "2024:06:15", "14:30:00", null, null,
                SampleDate, ChronoDateResolution.FullDate, SampleTime, null, 500L));
    }

    [Fact]
    public void FromRawDateWithTimeAndOffSetAndSubSec_Valid_CreatesCandidate()
    {
        var c = TimestampCandidateFactory.FromRawDateWithTimeAndOffSetAndSubSec(
            TimestampSourceType.Exif, TimestampRole.Created,
            "2024:06:15", "14:30:00", "+02:00", "500000000",
            SampleDate, ChronoDateResolution.FullDate, SampleTime, TimeSpan.FromHours(2), 500_000_000L);
        Assert.Equal(SampleDate, c.Date);
        Assert.Equal(SampleTime, c.Time);
        Assert.Equal(TimeSpan.FromHours(2), c.Offset);
        Assert.Equal(500_000_000L, c.SubSeconds);
    }

    [Fact]
    public void FromRawDateWithTimeAndOffSetAndSubSec_SubSecOverride_OverridesTimeFraction()
    {
        var timeWithFractions = new TimeOnly(14, 30, 0).Add(TimeSpan.FromTicks(5000));
        var c = TimestampCandidateFactory.FromRawDateWithTimeAndOffSetAndSubSec(
            TimestampSourceType.Exif, TimestampRole.Created,
            "2024:06:15", "14:30:00.5", null, "600000000",
            SampleDate, ChronoDateResolution.FullDate, timeWithFractions, null, 600_000_000L);
        Assert.Equal(600_000_000L, c.SubSeconds);
    }

    // ── FromRawDateTimeWithOffsetAndSubSec ──

    [Fact]
    public void FromRawDateTimeWithOffsetAndSubSec_AllEmpty_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            TimestampCandidateFactory.FromRawDateTimeWithOffsetAndSubSec(
                TimestampSourceType.Exif, TimestampRole.Created,
                "  ", null, null,
                DateTime.Now, null, null));
    }

    [Fact]
    public void FromRawDateTimeWithOffsetAndSubSec_OffsetWithoutRaw_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            TimestampCandidateFactory.FromRawDateTimeWithOffsetAndSubSec(
                TimestampSourceType.Exif, TimestampRole.Created,
                "2024-06-15 14:30:00", null, null,
                new DateTime(2024, 6, 15, 14, 30, 0), TimeSpan.FromHours(2), null));
    }

    [Fact]
    public void FromRawDateTimeWithOffsetAndSubSec_SubSecWithoutRaw_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            TimestampCandidateFactory.FromRawDateTimeWithOffsetAndSubSec(
                TimestampSourceType.Exif, TimestampRole.Created,
                "2024-06-15 14:30:00", null, null,
                new DateTime(2024, 6, 15, 14, 30, 0), null, 500L));
    }

    [Fact]
    public void FromRawDateTimeWithOffsetAndSubSec_Valid_CreatesCandidate()
    {
        var dt = new DateTime(2024, 6, 15, 14, 30, 0);
        var c = TimestampCandidateFactory.FromRawDateTimeWithOffsetAndSubSec(
            TimestampSourceType.Exif, TimestampRole.Created,
            "2024-06-15 14:30:00", "+02:00", "500000000",
            dt, TimeSpan.FromHours(2), 500_000_000L);
        Assert.Equal(SampleDate, c.Date);
        Assert.Equal(SampleTime, c.Time);
        Assert.Equal(TimeSpan.FromHours(2), c.Offset);
        Assert.Equal(500_000_000L, c.SubSeconds);
    }

    [Fact]
    public void FromRawDateTimeWithOffsetAndSubSec_SubSecOverride_OverridesDTFraction()
    {
        var dtWithFractions = new DateTime(2024, 6, 15, 14, 30, 0, DateTimeKind.Unspecified).AddTicks(5000);
        var c = TimestampCandidateFactory.FromRawDateTimeWithOffsetAndSubSec(
            TimestampSourceType.Exif, TimestampRole.Created,
            "2024-06-15 14:30:00.5", null, "600000000",
            dtWithFractions, null, 600_000_000L);
        Assert.Equal(600_000_000L, c.SubSeconds);
    }

    // ── FromDateTimeOffsetWithSubSec ──

    [Fact]
    public void FromDateTimeOffsetWithSubSec_AllEmpty_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            TimestampCandidateFactory.FromDateTimeOffsetWithSubSec(
                TimestampSourceType.Exif, TimestampRole.Created,
                "  ", null,
                DateTimeOffset.Now, null));
    }

    [Fact]
    public void FromDateTimeOffsetWithSubSec_SubSecWithoutRaw_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            TimestampCandidateFactory.FromDateTimeOffsetWithSubSec(
                TimestampSourceType.Exif, TimestampRole.Created,
                "2024-06-15T14:30:00+02:00", null,
                new DateTimeOffset(2024, 6, 15, 14, 30, 0, TimeSpan.FromHours(2)), 500L));
    }

    [Fact]
    public void FromDateTimeOffsetWithSubSec_Valid_CreatesCandidate()
    {
        var dto = new DateTimeOffset(2024, 6, 15, 14, 30, 0, TimeSpan.FromHours(2));
        var c = TimestampCandidateFactory.FromDateTimeOffsetWithSubSec(
            TimestampSourceType.Exif, TimestampRole.Created,
            "2024-06-15T14:30:00+02:00", "500000000",
            dto, 500_000_000L);
        Assert.Equal(SampleDate, c.Date);
        Assert.Equal(SampleTime, c.Time);
        Assert.Equal(TimeSpan.FromHours(2), c.Offset);
        Assert.Equal(500_000_000L, c.SubSeconds);
    }

    [Fact]
    public void FromDateTimeOffsetWithSubSec_SubSecOverride_OverridesDTOFraction()
    {
        var dtoWithFractions = new DateTimeOffset(2024, 6, 15, 14, 30, 0, TimeSpan.Zero)
            .AddTicks(5000);
        var c = TimestampCandidateFactory.FromDateTimeOffsetWithSubSec(
            TimestampSourceType.Exif, TimestampRole.Created,
            "2024-06-15T14:30:00Z", "600000000",
            dtoWithFractions, 600_000_000L);
        Assert.Equal(600_000_000L, c.SubSeconds);
    }
}
