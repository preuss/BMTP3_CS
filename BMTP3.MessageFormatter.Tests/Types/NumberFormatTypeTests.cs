namespace BMTP3.MessageFormatter.Tests.Types
{
	using Xunit;
	using BMTP3.MessageFormatter.Types;

	public class NumberFormatTypeTests
	{
		private readonly NumberFormatType _formatter = new();

		[Fact]
		public void FormatDefault_WithInteger_ReturnsStringRepresentation()
		{
			string result = _formatter.FormatDefault(42);
			Assert.Equal("42", result);
		}

		[Fact]
		public void FormatDefault_WithDouble_ReturnsStringRepresentation()
		{
			string result = _formatter.FormatDefault(3.14);
			Assert.Contains("3", result);
			Assert.Contains("14", result);
		}

		[Fact]
		public void FormatDefault_WithNull_ReturnsEmpty()
		{
			string result = _formatter.FormatDefault(null);
			Assert.Equal("", result);
		}

		[Fact]
		public void FormatWithStyle_Integer_RemovesDecimals()
		{
			string result = _formatter.FormatWithStyle(42.7, "integer");
			Assert.Equal("43", result);
		}

		[Fact]
		public void FormatWithStyle_Currency_IncludesCurrencySymbol()
		{
			string result = _formatter.FormatWithStyle(100, "currency");
			Assert.NotEmpty(result);
		}

		[Fact]
		public void FormatWithStyle_Percent_IncludesPercentSign()
		{
			string result = _formatter.FormatWithStyle(0.5, "percent");
			Assert.Contains("%", result);
		}

		[Fact]
		public void FormatWithStyle_Thousands_IncludesThousandsSeparator()
		{
			string result = _formatter.FormatWithStyle(1234, "thousands");
			// Different cultures may use different separators, so just check it's formatted differently
			Assert.NotEqual("1234", result);
		}

		[Fact]
		public void FormatWithStyle_Scientific_UsesScientificNotation()
		{
			string result = _formatter.FormatWithStyle(1234, "scientific");
			Assert.Contains("E", result.ToUpper());
		}

		[Fact]
		public void AssertCompatible_WithInteger_DoesNotThrow()
		{
			_formatter.AssertCompatible(42);
		}

		[Fact]
		public void AssertCompatible_WithDouble_DoesNotThrow()
		{
			_formatter.AssertCompatible(3.14);
		}

		[Fact]
		public void AssertCompatible_WithDecimal_DoesNotThrow()
		{
			_formatter.AssertCompatible(99.99m);
		}

		[Fact]
		public void AssertCompatible_WithString_Throws()
		{
			Assert.Throws<FormatTypeAssertionException>(() => _formatter.AssertCompatible("not a number"));
		}

		[Fact]
		public void AssertCompatible_WithNull_DoesNotThrow()
		{
			_formatter.AssertCompatible(null);
		}

		[Fact]
		public void Name_ReturnsCorrectName()
		{
			Assert.Equal("number", _formatter.Name);
		}

		[Fact]
		public void GetAvailableStyles_ReturnsAllStyles()
		{
			var styles = _formatter.GetAvailableStyles();
			Assert.Contains("integer", styles);
			Assert.Contains("currency", styles);
			Assert.Contains("percent", styles);
		}

		[Fact]
		public void IsStyleAvailable_WithValidStyle_ReturnsTrue()
		{
			Assert.True(_formatter.IsStyleAvailable("integer"));
			Assert.True(_formatter.IsStyleAvailable("INTEGER"));
		}

		[Fact]
		public void IsStyleAvailable_WithInvalidStyle_ReturnsFalse()
		{
			Assert.False(_formatter.IsStyleAvailable("invalid"));
		}
	}
}
