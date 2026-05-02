namespace BMTP3.MessageFormatter.Tests.Types
{
	using Xunit;
	using BMTP3.MessageFormatter.Types;

	public class DateTimeFormatTypeTests
	{
		private readonly DateFormatType _dateFormatter = new();
		private readonly DateTimeFormatType _dateTimeFormatter = new();
		private readonly TimeFormatType _timeFormatter = new();

		#region DateFormatType Tests

		[Fact]
		public void DateFormatDefault_WithDateTime_ReturnsFormattedDate()
		{
			DateTime date = new(2025, 4, 17);
			string result = _dateFormatter.FormatDefault(date);
			Assert.NotEmpty(result);
		}

		[Fact]
		public void DateFormatDefault_WithNull_ReturnsEmpty()
		{
			string result = _dateFormatter.FormatDefault(null);
			Assert.Equal("", result);
		}

		[Fact]
		public void DateFormatWithStyle_Short_ReturnsFormatted()
		{
			DateTime date = new(2025, 4, 17);
			string result = _dateFormatter.FormatWithStyle(date, "short");
			Assert.NotEmpty(result);
		}

		[Fact]
		public void DateFormatWithStyle_Long_ReturnsFormatted()
		{
			DateTime date = new(2025, 4, 17);
			string result = _dateFormatter.FormatWithStyle(date, "long");
			Assert.NotEmpty(result);
		}

		[Fact]
		public void DateAssertCompatible_WithDateTime_DoesNotThrow()
		{
			_dateFormatter.AssertCompatible(new DateTime(2025, 4, 17));
		}

		[Fact]
		public void DateAssertCompatible_WithString_Throws()
		{
			Assert.Throws<FormatTypeAssertionException>(() => _dateFormatter.AssertCompatible("2025-04-17"));
		}

		[Fact]
		public void DateAssertCompatible_WithNull_DoesNotThrow()
		{
			_dateFormatter.AssertCompatible(null);
		}

		[Fact]
		public void DateName_ReturnsCorrectName()
		{
			Assert.Equal("date", _dateFormatter.Name);
		}

		#endregion

		#region DateTimeFormatType Tests

		[Fact]
		public void DateTimeFormatDefault_WithDateTime_ReturnsFormattedDateTime()
		{
			DateTime dateTime = new(2025, 4, 17, 14, 30, 0);
			string result = _dateTimeFormatter.FormatDefault(dateTime);
			Assert.NotEmpty(result);
		}

		[Fact]
		public void DateTimeFormatWithStyle_Short_ReturnsFormatted()
		{
			DateTime dateTime = new(2025, 4, 17, 14, 30, 0);
			string result = _dateTimeFormatter.FormatWithStyle(dateTime, "short");
			Assert.NotEmpty(result);
		}

		[Fact]
		public void DateTimeAssertCompatible_WithDateTime_DoesNotThrow()
		{
			_dateTimeFormatter.AssertCompatible(new DateTime(2025, 4, 17, 14, 30, 0));
		}

		[Fact]
		public void DateTimeAssertCompatible_WithString_Throws()
		{
			Assert.Throws<FormatTypeAssertionException>(() => _dateTimeFormatter.AssertCompatible("2025-04-17T14:30:00"));
		}

		[Fact]
		public void DateTimeName_ReturnsCorrectName()
		{
			Assert.Equal("datetime", _dateTimeFormatter.Name);
		}

		#endregion

		#region TimeFormatType Tests

		[Fact]
		public void TimeFormatDefault_WithDateTime_ReturnsFormattedTime()
		{
			DateTime time = new(2025, 4, 17, 14, 30, 45);
			string result = _timeFormatter.FormatDefault(time);
			Assert.NotEmpty(result);
		}

		[Fact]
		public void TimeFormatDefault_WithTimeOnly_ReturnsFormattedTime()
		{
			TimeOnly time = new(14, 30, 45);
			string result = _timeFormatter.FormatDefault(time);
			Assert.NotEmpty(result);
		}

		[Fact]
		public void TimeFormatWithStyle_Short_ReturnsFormatted()
		{
			DateTime time = new(2025, 4, 17, 14, 30, 45);
			string result = _timeFormatter.FormatWithStyle(time, "short");
			Assert.NotEmpty(result);
		}

		[Fact]
		public void TimeFormatWithStyle_CaseInsensitive_Works()
		{
			DateTime time = new(2025, 4, 17, 14, 30, 45);
			string resultLower = _timeFormatter.FormatWithStyle(time, "short");
			string resultUpper = _timeFormatter.FormatWithStyle(time, "SHORT");
			Assert.Equal(resultLower, resultUpper);
		}

		[Fact]
		public void TimeAssertCompatible_WithDateTime_DoesNotThrow()
		{
			_timeFormatter.AssertCompatible(new DateTime(2025, 4, 17, 14, 30, 45));
		}

		[Fact]
		public void TimeAssertCompatible_WithTimeOnly_DoesNotThrow()
		{
			_timeFormatter.AssertCompatible(new TimeOnly(14, 30, 45));
		}

		[Fact]
		public void TimeAssertCompatible_WithString_Throws()
		{
			Assert.Throws<FormatTypeAssertionException>(() => _timeFormatter.AssertCompatible("14:30:45"));
		}

		[Fact]
		public void TimeName_ReturnsCorrectName()
		{
			Assert.Equal("time", _timeFormatter.Name);
		}

		[Fact]
		public void TimeGetAvailableStyles_ReturnsAllStyles()
		{
			var styles = _timeFormatter.GetAvailableStyles();
			Assert.Contains("short", styles);
			Assert.Contains("medium", styles);
			Assert.Contains("long", styles);
			Assert.Contains("full", styles);
		}

		[Fact]
		public void TimeIsStyleAvailable_WithValidStyle_ReturnsTrue()
		{
			Assert.True(_timeFormatter.IsStyleAvailable("short"));
			Assert.True(_timeFormatter.IsStyleAvailable("SHORT"));
		}

		[Fact]
		public void TimeIsStyleAvailable_WithInvalidStyle_ReturnsFalse()
		{
			Assert.False(_timeFormatter.IsStyleAvailable("invalid"));
		}

		#endregion
	}
}
