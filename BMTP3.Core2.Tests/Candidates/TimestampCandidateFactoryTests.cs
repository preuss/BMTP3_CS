using BMTP3.Core2.BackupNew.candidates;
using BMTP3.Core2.BackupNew.exifreader.parsers;

namespace BMTP3.Core2.Tests.Candidates;

public class TimestampCandidateFactoryTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static TimestampCandidate MakeFromDateTime(DateTime dt)
        => TimestampCandidateFactory.FromDateTime(TimestampSourceType.Exif, TimestampRole.Created, dt);

    private static TimestampCandidate MakeFromDateTimeOffset(DateTimeOffset dto)
        => TimestampCandidateFactory.FromDateTimeOffset(TimestampSourceType.Exif, TimestampRole.Created, dto);

    // ── TrimToNanoseconds ────────────────────────────────────────────────────

    [Fact]
    public void TrimToNanoseconds_NullInput_ReturnsNull()
    {
        long? result = TimestampCandidateFactory.TrimToNanoseconds((long?)null);

        Assert.Null(result);
    }

    [Fact]
    public void TrimToNanoseconds_WholeSecondTicks_ReturnsNull()
    {
        // Exactly 1 second worth of ticks → sub-second fraction is zero → null
        long wholeTicks = TimeSpan.TicksPerSecond; // 10_000_000
        long? result = TimestampCandidateFactory.TrimToNanoseconds((long?)wholeTicks);

        Assert.Null(result);
    }

    [Fact]
    public void TrimToNanoseconds_HalfSecondTicks_ReturnsExpectedNanoseconds()
    {
        // 5_000_000 ticks = 0.5 s → 5_000_000 * 100 = 500_000_000 ns
        long halfSecondTicks = 5_000_000L;
        long? result = TimestampCandidateFactory.TrimToNanoseconds((long?)halfSecondTicks);

        Assert.Equal(500_000_000L, result);
    }

    // ── OffsetToTimeSpan ─────────────────────────────────────────────────────

    [Fact]
    public void OffsetToTimeSpan_UtcDateTime_ReturnsZero()
    {
        DateTime utcDt = new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        TimeSpan? result = TimestampCandidateFactory.OffsetToTimeSpan(utcDt);

        Assert.Equal(TimeSpan.Zero, result);
    }

    [Fact]
    public void OffsetToTimeSpan_UnspecifiedDateTime_ReturnsNull()
    {
        DateTime unspecified = new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Unspecified);
        TimeSpan? result = TimestampCandidateFactory.OffsetToTimeSpan(unspecified);

        Assert.Null(result);
    }

    [Fact]
    public void OffsetToTimeSpan_DateTimeOffset_ReturnsExactOffset()
    {
        TimeSpan expectedOffset = TimeSpan.FromHours(2);
        DateTimeOffset dto = new DateTimeOffset(2024, 6, 1, 14, 0, 0, expectedOffset);
        TimeSpan? result = TimestampCandidateFactory.OffsetToTimeSpan(dto);

        Assert.Equal(expectedOffset, result);
    }

    // ── FromDateTime ─────────────────────────────────────────────────────────

    [Fact]
    public void FromDateTime_UtcKind_CandidateDateTimeAndOffsetAreCorrect()
    {
        DateTime dt = new DateTime(2023, 3, 15, 10, 30, 45, DateTimeKind.Utc);
        TimestampCandidate candidate = MakeFromDateTime(dt);

        Assert.Equal(new DateOnly(2023, 3, 15), candidate.Date);
        Assert.Equal(new TimeOnly(10, 30, 45), candidate.Time);
        Assert.Equal(TimeSpan.Zero, candidate.Offset);
        Assert.Equal(ChronoDateResolution.FullDate, candidate.DateResolution);
    }

    [Fact]
    public void FromDateTime_UnspecifiedKind_OffsetIsNull()
    {
        DateTime dt = new DateTime(2023, 3, 15, 10, 30, 45, DateTimeKind.Unspecified);
        TimestampCandidate candidate = MakeFromDateTime(dt);

        Assert.Null(candidate.Offset);
    }

    [Fact]
    public void FromDateTime_WithSubSecondFractions_SubSecondsExtractedAndTimeStripped()
    {
        // 500ms = 5_000_000 ticks sub-second → 500_000_000 ns
        DateTime dt = new DateTime(2023, 1, 1, 12, 0, 0, 500, DateTimeKind.Utc);
        TimestampCandidate candidate = MakeFromDateTime(dt);

        Assert.Equal(new TimeOnly(12, 0, 0), candidate.Time);  // no fractions on TimeOnly
        Assert.Equal(500_000_000L, candidate.SubSeconds);
    }

    // ── FromDateTimeOffset ───────────────────────────────────────────────────

    [Fact]
    public void FromDateTimeOffset_PreservesOffset()
    {
        TimeSpan offset = TimeSpan.FromHours(2);
        DateTimeOffset dto = new DateTimeOffset(2024, 8, 20, 16, 45, 30, offset);
        TimestampCandidate candidate = MakeFromDateTimeOffset(dto);

        Assert.Equal(offset, candidate.Offset);
        Assert.Equal(new DateOnly(2024, 8, 20), candidate.Date);
        Assert.Equal(new TimeOnly(16, 45, 30), candidate.Time);
    }

    [Fact]
    public void FromDateTimeOffset_NegativeOffset_PreservedCorrectly()
    {
        TimeSpan offset = TimeSpan.FromHours(-5);
        DateTimeOffset dto = new DateTimeOffset(2024, 11, 5, 9, 0, 0, offset);
        TimestampCandidate candidate = MakeFromDateTimeOffset(dto);

        Assert.Equal(offset, candidate.Offset);
    }

    // ── FromRawTimestamp / ConvertTimestampUtcToDateTimeOffset ───────────────

    [Fact]
    public void ConvertTimestampUtcToDateTimeOffset_UnixEpochSeconds_ReturnsKnownDateTimeOffset()
    {
        // Unix timestamp 0 = 1970-01-01T00:00:00Z
        DateTimeOffset result = TimestampCandidateFactory.ConvertTimestampUtcToDateTimeOffset(
            0L, EpochType.Unix, TimestampResolution.Seconds);

        Assert.Equal(DateTimeOffset.UnixEpoch, result);
    }

    [Fact]
    public void ConvertTimestampUtcToDateTimeOffset_UnixEpochMilliseconds_ReturnsKnownDateTimeOffset()
    {
        // 1000 ms = 1970-01-01T00:00:01Z
        DateTimeOffset result = TimestampCandidateFactory.ConvertTimestampUtcToDateTimeOffset(
            1000L, EpochType.Unix, TimestampResolution.Milliseconds);

        Assert.Equal(DateTimeOffset.UnixEpoch.AddSeconds(1), result);
    }

    [Fact]
    public void FromRawTimestamp_UnixSeconds_CandidateDateTimeMatchesEpochConversion()
    {
        long unixSeconds = 1_700_000_000L; // 2023-11-14T22:13:20Z
        TimestampCandidate candidate = TimestampCandidateFactory.FromRawTimestamp(
            TimestampSourceType.FileSystem,
            TimestampRole.Modified,
            rawTimestamp: "1700000000",
            timestampUtc: unixSeconds,
            epoch: EpochType.Unix,
            resolution: TimestampResolution.Seconds);

        DateTimeOffset expected = DateTimeOffset.UnixEpoch.AddSeconds(unixSeconds);

        Assert.Equal(DateOnly.FromDateTime(expected.UtcDateTime), candidate.Date);
        Assert.Equal(new TimeOnly(expected.Hour, expected.Minute, expected.Second), candidate.Time);
        Assert.Equal(TimeSpan.Zero, candidate.Offset);
        Assert.Equal("1700000000", candidate.Source.Timestamp);
    }

    // ── FromRawDate ───────────────────────────────────────────────────────────

    [Fact]
    public void FromRawDate_ValidDate_CandidateContainsOnlyDateFields()
    {
        DateOnly date = new DateOnly(2022, 7, 4);
        TimestampCandidate candidate = TimestampCandidateFactory.FromRawDate(
            TimestampSourceType.Exif,
            TimestampRole.Created,
            rawDate: "2022:07:04",
            date: date,
            dateResolution: ChronoDateResolution.FullDate);

        Assert.Equal(date, candidate.Date);
        Assert.Equal(ChronoDateResolution.FullDate, candidate.DateResolution);
        Assert.Null(candidate.Time);
        Assert.Null(candidate.Offset);
        Assert.Null(candidate.SubSeconds);
        Assert.Equal("2022:07:04", candidate.Source.Date);
    }

    [Fact]
    public void FromRawDate_EmptyRawDate_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            TimestampCandidateFactory.FromRawDate(
                TimestampSourceType.Exif,
                TimestampRole.Created,
                rawDate: "   ",
                date: new DateOnly(2022, 1, 1),
                dateResolution: ChronoDateResolution.FullDate));
    }

    // ── FromRawTime ───────────────────────────────────────────────────────────

    [Fact]
    public void FromRawTime_ValidHHMMSS_CandidateContainsOnlyTimeFields()
    {
        TimeOnly time = new TimeOnly(14, 35, 59);
        TimestampCandidate candidate = TimestampCandidateFactory.FromRawTime(
            TimestampSourceType.Exif,
            TimestampRole.Created,
            rawTime: "14:35:59",
            time: time);

        Assert.Equal(time, candidate.Time);
        Assert.Null(candidate.Date);
        Assert.Null(candidate.Offset);
        Assert.Equal("14:35:59", candidate.Source.Time);
    }

    // ── FromRawOffset ─────────────────────────────────────────────────────────

    [Fact]
    public void FromRawOffset_PlusHHMM_CandidateContainsOnlyOffsetField()
    {
        TimeSpan offset = TimeSpan.FromHours(5) + TimeSpan.FromMinutes(30);
        TimestampCandidate candidate = TimestampCandidateFactory.FromRawOffset(
            TimestampSourceType.Exif,
            TimestampRole.Created,
            rawOffset: "+05:30",
            offset: offset);

        Assert.Equal(offset, candidate.Offset);
        Assert.Null(candidate.Date);
        Assert.Null(candidate.Time);
        Assert.Equal("+05:30", candidate.Source.Offset);
    }

    // ── FromRawDateTimeOffset guard ───────────────────────────────────────────

    [Fact]
    public void FromRawDateTimeOffset_EmptyRawString_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            TimestampCandidateFactory.FromRawDateTimeOffset(
                TimestampSourceType.Xmp,
                TimestampRole.Modified,
                rawDateTimeOffset: "",
                dto: DateTimeOffset.UtcNow));
    }
}
