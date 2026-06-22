using BMTP3.Core4.Engine.TimeStamp.Candidates;
using BMTP3.Core4.Engine.TimeStamp.Candidates.Parsing;

namespace BMTP3.Core4.Tests.Engine.TimeStamp.Candidates;

public class TimestampFormatDescriptorTests
{
    [Fact]
    public void Describe_Iso8601_DotFraction()
    {
        var d = TimestampFormatDescriptor.Describe(TimestampFormatStyle.Iso8601_DotFraction);
        Assert.Equal(DateComponentSeparator.Hyphen_Minus, d.DateYearMonthSeparator);
        Assert.Equal(DateComponentSeparator.Hyphen_Minus, d.DateMonthDaySeparator);
        Assert.Equal(TimeRepresentation.Clock, d.TimeFormat);
        Assert.Equal(TimeClockSeparator.Colon, d.TimeClockSeparator);
        Assert.Equal(TimeClockFormatStyle.Full, d.TimeClockFormatStyle);
        Assert.Equal(DecimalFractionSeparator.Dot, d.TimeFractionSeparator);
        Assert.Equal(DateTimeSeparatorStyle.T, d.DateTimeSeparatorStyle);
        Assert.Equal(TimeRepresentation.Clock, d.OffsetFormat);
        Assert.Equal(TimeClockSeparator.Colon, d.OffsetClockSeparator);
        Assert.Equal(TimeClockFormatStyle.Full, d.OffsetClockTrimStyle);
        Assert.Equal(DecimalFractionSeparator.Dot, d.OffsetFractionSeparator);
    }

    [Fact]
    public void Describe_Iso8601_DotFraction_Compact()
    {
        var d = TimestampFormatDescriptor.Describe(TimestampFormatStyle.Iso8601_DotFraction_Compact);
        Assert.Equal(DateComponentSeparator.None, d.DateYearMonthSeparator);
        Assert.Equal(DateComponentSeparator.None, d.DateMonthDaySeparator);
        Assert.Equal(TimeClockSeparator.None, d.TimeClockSeparator);
        Assert.Equal(DateTimeSeparatorStyle.T, d.DateTimeSeparatorStyle);
    }

    [Fact]
    public void Describe_Iso8601_CommaFraction()
    {
        var d = TimestampFormatDescriptor.Describe(TimestampFormatStyle.Iso8601_CommaFraction);
        Assert.Equal(DecimalFractionSeparator.Comma, d.TimeFractionSeparator);
        Assert.Equal(DecimalFractionSeparator.Comma, d.OffsetFractionSeparator);
    }

    [Fact]
    public void Describe_Iso8601_DotFraction_OffsetDuration()
    {
        var d = TimestampFormatDescriptor.Describe(TimestampFormatStyle.Iso8601_DotFraction_OffsetDuration);
        Assert.Equal(TimeRepresentation.Duration, d.OffsetFormat);
        Assert.Equal(DecimalFractionSeparator.Dot, d.OffsetFractionSeparator);
    }

    [Fact]
    public void Describe_Iso8601_CommaFraction_OffsetDuration()
    {
        var d = TimestampFormatDescriptor.Describe(TimestampFormatStyle.Iso8601_CommaFraction_OffsetDuration);
        Assert.Equal(TimeRepresentation.Duration, d.OffsetFormat);
        Assert.Equal(DecimalFractionSeparator.Comma, d.OffsetFractionSeparator);
    }

    [Fact]
    public void Describe_Compact_DotFraction()
    {
        var d = TimestampFormatDescriptor.Describe(TimestampFormatStyle.Compact_DotFraction);
        Assert.Equal(DateComponentSeparator.None, d.DateYearMonthSeparator);
        Assert.Equal(DateComponentSeparator.None, d.DateMonthDaySeparator);
        Assert.Equal(TimeClockSeparator.None, d.TimeClockSeparator);
        Assert.Equal(DateTimeSeparatorStyle.T, d.DateTimeSeparatorStyle);
        Assert.Equal(DecimalFractionSeparator.Dot, d.TimeFractionSeparator);
    }

    [Fact]
    public void Describe_Compact_CommaFraction()
    {
        var d = TimestampFormatDescriptor.Describe(TimestampFormatStyle.Compact_CommaFraction);
        Assert.Equal(DateComponentSeparator.None, d.DateYearMonthSeparator);
        Assert.Equal(DateComponentSeparator.None, d.DateMonthDaySeparator);
        Assert.Equal(TimeClockSeparator.None, d.TimeClockSeparator);
        Assert.Equal(DecimalFractionSeparator.Comma, d.TimeFractionSeparator);
        Assert.Equal(DecimalFractionSeparator.Comma, d.OffsetFractionSeparator);
    }

    [Fact]
    public void Describe_Compact_Underscore_DotFraction()
    {
        var d = TimestampFormatDescriptor.Describe(TimestampFormatStyle.Compact_Underscore_DotFraction);
        Assert.Equal(DateComponentSeparator.None, d.DateYearMonthSeparator);
        Assert.Equal(DateComponentSeparator.None, d.DateMonthDaySeparator);
        Assert.Equal(TimeClockSeparator.None, d.TimeClockSeparator);
        Assert.Equal(DateTimeSeparatorStyle.Underscore, d.DateTimeSeparatorStyle);
        Assert.Equal(DecimalFractionSeparator.Dot, d.TimeFractionSeparator);
    }

    [Fact]
    public void Describe_Compact_Underscore_CommaFraction()
    {
        var d = TimestampFormatDescriptor.Describe(TimestampFormatStyle.Compact_Underscore_CommaFraction);
        Assert.Equal(DateComponentSeparator.None, d.DateYearMonthSeparator);
        Assert.Equal(DateComponentSeparator.None, d.DateMonthDaySeparator);
        Assert.Equal(TimeClockSeparator.None, d.TimeClockSeparator);
        Assert.Equal(DateTimeSeparatorStyle.Underscore, d.DateTimeSeparatorStyle);
        Assert.Equal(DecimalFractionSeparator.Comma, d.TimeFractionSeparator);
        Assert.Equal(DecimalFractionSeparator.Comma, d.OffsetFractionSeparator);
    }

    [Fact]
    public void Describe_DotTime_DotFraction()
    {
        var d = TimestampFormatDescriptor.Describe(TimestampFormatStyle.DotTime_DotFraction);
        Assert.Equal(DateComponentSeparator.Hyphen_Minus, d.DateYearMonthSeparator);
        Assert.Equal(DateComponentSeparator.Hyphen_Minus, d.DateMonthDaySeparator);
        Assert.Equal(TimeClockSeparator.Dot, d.TimeClockSeparator);
        Assert.Equal(TimeClockSeparator.Dot, d.OffsetClockSeparator);
        Assert.Equal(DateTimeSeparatorStyle.T, d.DateTimeSeparatorStyle);
        Assert.Equal(DecimalFractionSeparator.Dot, d.TimeFractionSeparator);
    }

    [Fact]
    public void Describe_DotTime_CommaFraction()
    {
        var d = TimestampFormatDescriptor.Describe(TimestampFormatStyle.DotTime_CommaFraction);
        Assert.Equal(TimeClockSeparator.Dot, d.TimeClockSeparator);
        Assert.Equal(DecimalFractionSeparator.Comma, d.TimeFractionSeparator);
        Assert.Equal(DecimalFractionSeparator.Comma, d.OffsetFractionSeparator);
    }

    [Fact]
    public void Describe_DotDateTime_Underscore_DotFraction()
    {
        var d = TimestampFormatDescriptor.Describe(TimestampFormatStyle.DotDateTime_Underscore_DotFraction);
        Assert.Equal(DateComponentSeparator.Dot, d.DateYearMonthSeparator);
        Assert.Equal(DateComponentSeparator.Dot, d.DateMonthDaySeparator);
        Assert.Equal(TimeClockSeparator.Dot, d.TimeClockSeparator);
        Assert.Equal(DateTimeSeparatorStyle.Underscore, d.DateTimeSeparatorStyle);
    }

    [Fact]
    public void Describe_DotDateTime_Underscore_CommaFraction()
    {
        var d = TimestampFormatDescriptor.Describe(TimestampFormatStyle.DotDateTime_Underscore_CommaFraction);
        Assert.Equal(DateComponentSeparator.Dot, d.DateYearMonthSeparator);
        Assert.Equal(DateComponentSeparator.Dot, d.DateMonthDaySeparator);
        Assert.Equal(DateTimeSeparatorStyle.Underscore, d.DateTimeSeparatorStyle);
        Assert.Equal(DecimalFractionSeparator.Comma, d.TimeFractionSeparator);
    }

    [Fact]
    public void Describe_DotDateTime_DotFraction()
    {
        var d = TimestampFormatDescriptor.Describe(TimestampFormatStyle.DotDateTime_DotFraction);
        Assert.Equal(DateComponentSeparator.Dot, d.DateYearMonthSeparator);
        Assert.Equal(DateComponentSeparator.Dot, d.DateMonthDaySeparator);
        Assert.Equal(TimeClockSeparator.Dot, d.TimeClockSeparator);
        Assert.Equal(DateTimeSeparatorStyle.T, d.DateTimeSeparatorStyle);
    }

    [Fact]
    public void Describe_DotDateTime_CommaFraction()
    {
        var d = TimestampFormatDescriptor.Describe(TimestampFormatStyle.DotDateTime_CommaFraction);
        Assert.Equal(DateComponentSeparator.Dot, d.DateYearMonthSeparator);
        Assert.Equal(DateComponentSeparator.Dot, d.DateMonthDaySeparator);
        Assert.Equal(TimeClockSeparator.Dot, d.TimeClockSeparator);
        Assert.Equal(DateTimeSeparatorStyle.T, d.DateTimeSeparatorStyle);
        Assert.Equal(DecimalFractionSeparator.Comma, d.TimeFractionSeparator);
    }

    [Fact]
    public void Describe_DanishSeparator_CommaFraction()
    {
        var d = TimestampFormatDescriptor.Describe(TimestampFormatStyle.DanishSeparator_CommaFraction);
        Assert.Equal(DateComponentSeparator.Hyphen_Minus, d.DateYearMonthSeparator);
        Assert.Equal(DateComponentSeparator.Slash, d.DateMonthDaySeparator);
        Assert.Equal(TimeClockSeparator.Colon, d.TimeClockSeparator);
        Assert.Equal(DateTimeSeparatorStyle.T, d.DateTimeSeparatorStyle);
        Assert.Equal(DecimalFractionSeparator.Comma, d.TimeFractionSeparator);
    }

    [Fact]
    public void Describe_DanishSeparator_Underscore_CommaFraction()
    {
        var d = TimestampFormatDescriptor.Describe(TimestampFormatStyle.DanishSeparator_Underscore_CommaFraction);
        Assert.Equal(DateComponentSeparator.Hyphen_Minus, d.DateYearMonthSeparator);
        Assert.Equal(DateComponentSeparator.Slash, d.DateMonthDaySeparator);
        Assert.Equal(DateTimeSeparatorStyle.Underscore, d.DateTimeSeparatorStyle);
        Assert.Equal(DecimalFractionSeparator.Comma, d.TimeFractionSeparator);
    }

    [Fact]
    public void Describe_UnknownStyle_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TimestampFormatDescriptor.Describe((TimestampFormatStyle)999));
    }
}
