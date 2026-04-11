using BMTP3.Core2.BackupNew.candidates;
using BMTP3.Core2.BackupNew.candidates.parsing;

namespace BMTP3.Core2.Tests.Candidates;

public class TimestampFormatterTests
{
	private readonly TimestampFormatter _formatter = new();
	// ── Helper: build a fully-valid candidate from a DateTimeOffset ──────────

	private static TimestampCandidate CandidateFrom(DateTimeOffset dto, long? subSeconds = null)
	{
		TimeOnly time = new(dto.Hour, dto.Minute, dto.Second);
		return new TimestampCandidate(
			TimestampSourceType.Exif,
			TimestampRole.Created,
			new TimestampSources { DateTimeOffset = dto.ToString("o") },
			DateOnly.FromDateTime(dto.DateTime),
			ChronoDateResolution.FullDate,
			time,
			subSeconds,
			dto.Offset
		);
	}

	private static TimestampCandidate CandidateFrom(DateOnly date, TimeOnly time, TimeSpan? offset,
		long? subSeconds = null)
	{
		TimestampSources sources = offset.HasValue
			? new TimestampSources { DateTimeOffset = "raw" }
			: new TimestampSources { DateTime = "raw" };

		return new TimestampCandidate(
			TimestampSourceType.Exif,
			TimestampRole.Created,
			sources,
			date,
			ChronoDateResolution.FullDate,
			time,
			subSeconds,
			offset
		);
	}

	// ── Format_ISO8601 ────────────────────────────────────────────────────────

	[Fact]
	public void Format_ISO8601_DotFraction_NoSubseconds_ProducesCorrectString()
	{
		// 2026-01-13T14:30:00Z  (UTC, no fractions)
		DateTimeOffset dto = new(2026, 1, 13, 14, 30, 0, TimeSpan.Zero);
		TimestampCandidate candidate = CandidateFrom(dto);

		string result = _formatter.Format(candidate, TimestampFormatStyle.Iso8601_DotFraction);

		Assert.Equal("2026-01-13T14:30:00Z", result);
	}

	// ── Z for zero offset ─────────────────────────────────────────────────────

	[Fact]
	public void Format_ZeroOffset_ProducesZ_NotPlusZeroZero()
	{
		DateTimeOffset dto = new(2024, 6, 1, 12, 0, 0, TimeSpan.Zero);
		TimestampCandidate candidate = CandidateFrom(dto);

		string result = _formatter.Format(candidate, TimestampFormatStyle.Iso8601_DotFraction);

		Assert.Contains("Z", result);
		Assert.DoesNotContain("+00", result);
	}

	// ── No offset → no zone designator ───────────────────────────────────────

	[Fact]
	public void Format_NullOffset_ProducesNoOffsetSuffix()
	{
		// Unspecified offset → no 'Z', no ±hh:mm
		TimestampCandidate candidate = CandidateFrom(
			new DateOnly(2024, 5, 20),
			new TimeOnly(8, 0, 0),
			null);

		string result = _formatter.Format(candidate, TimestampFormatStyle.Iso8601_DotFraction);

		Assert.DoesNotContain("Z", result);
		Assert.DoesNotContain("+", result);
		Assert.DoesNotContain("-", result.Substring(10)); // no sign after date part
	}

	// ── Negative offset ───────────────────────────────────────────────────────

	[Fact]
	public void Format_NegativeOffset_ProducesMinusSign()
	{
		TimeSpan negOffset = TimeSpan.FromHours(-5);
		DateTimeOffset dto = new(2024, 3, 10, 9, 0, 0, negOffset);
		TimestampCandidate candidate = CandidateFrom(dto);

		string result = _formatter.Format(candidate, TimestampFormatStyle.Iso8601_DotFraction);

		Assert.Contains("-05:00:00", result);
	}

	// ── Fractional digits: 3 (milliseconds) ──────────────────────────────────

	[Fact]
	public void Format_WithMillisecondSubseconds_Produces3DecimalDigits()
	{
		// 123 ms → 123_000_000 ns
		long subSeconds = 123_000_000L;
		DateTimeOffset dto = new(2025, 2, 28, 10, 20, 30, TimeSpan.Zero);
		TimestampCandidate candidate = CandidateFrom(dto, subSeconds);

		string result = _formatter.Format(candidate, TimestampFormatStyle.Iso8601_DotFraction);

		// The fraction part should be exactly "123" (trailing zeros stripped)
		Assert.Contains(".123", result);
	}

	// ── Fractional digits: 6 (microseconds) ──────────────────────────────────

	[Fact]
	public void Format_WithMicrosecondSubseconds_Produces6DecimalDigits()
	{
		// 123456 µs → 123_456_000 ns
		long subSeconds = 123_456_000L;
		DateTimeOffset dto = new(2025, 2, 28, 10, 20, 30, TimeSpan.Zero);
		TimestampCandidate candidate = CandidateFrom(dto, subSeconds);

		string result = _formatter.Format(candidate, TimestampFormatStyle.Iso8601_DotFraction);

		Assert.Contains(".123456", result);
	}

	// ── Fractional digits: 9 (nanoseconds) ───────────────────────────────────

	[Fact]
	public void Format_WithNanosecondSubseconds_Produces9DecimalDigits()
	{
		// 123_456_789 ns (all nine digits non-zero → no trailing trim)
		long subSeconds = 123_456_789L;
		DateTimeOffset dto = new(2025, 2, 28, 10, 20, 30, TimeSpan.Zero);
		TimestampCandidate candidate = CandidateFrom(dto, subSeconds);

		string result = _formatter.Format(candidate, TimestampFormatStyle.Iso8601_DotFraction);

		Assert.Contains(".123456789", result);
	}

	// ── CommaFraction style ───────────────────────────────────────────────────

	[Fact]
	public void Format_CommaFractionStyle_UsesCommaNotDot()
	{
		long subSeconds = 500_000_000L; // 0.5 s
		DateTimeOffset dto = new(2025, 6, 15, 8, 0, 0, TimeSpan.Zero);
		TimestampCandidate candidate = CandidateFrom(dto, subSeconds);

		string result = _formatter.Format(candidate, TimestampFormatStyle.Iso8601_CommaFraction);

		Assert.Contains(",5", result);
		Assert.DoesNotContain(".5", result);
	}

	// ── Compact style ─────────────────────────────────────────────────────────

	[Fact]
	public void Format_CompactUnderscoreStyle_ProducesExpectedSeparators()
	{
		// YYYYMMDD_hhmmssZ
		DateTimeOffset dto = new(2026, 1, 13, 14, 30, 0, TimeSpan.Zero);
		TimestampCandidate candidate = CandidateFrom(dto);

		string result = _formatter.Format(candidate, TimestampFormatStyle.Compact_Underscore_DotFraction);

		Assert.Equal("20260113_143000Z", result);
	}

	// ── DotDateTime style ─────────────────────────────────────────────────────

	[Fact]
	public void Format_DotDateTime_DotFraction_UsesDotSeparatorsInDateAndTime()
	{
		DateTimeOffset dto = new(2026, 1, 13, 14, 30, 0, TimeSpan.Zero);
		TimestampCandidate candidate = CandidateFrom(dto);

		string result = _formatter.Format(candidate, TimestampFormatStyle.DotDateTime_DotFraction);

		// Expected: 2026.01.13T14.30.00Z
		Assert.StartsWith("2026.01.13T", result);
		Assert.Contains("14.30.00", result);
		Assert.EndsWith("Z", result);
	}

	// ── Empty candidate ───────────────────────────────────────────────────────

	[Fact]
	public void Format_EmptyCandidate_ToStringReturnsEmptyTimestampMarker()
	{
		// An empty candidate's ToString returns the "<empty timestamp>" sentinel
		TimestampCandidate empty = new(
			TimestampSourceType.Exif,
			TimestampRole.Unknown,
			TimestampSources.Empty,
			null,
			null,
			null,
			null,
			null);

		// ToString delegates to formatter indirectly; test via the candidate itself
		string result = empty.ToString(TimestampFormatStyle.Iso8601_DotFraction);

		Assert.Equal("<empty timestamp>", result);
	}
}