namespace BMTP3.MessageFormatter.Tests.Functions
{
	using Xunit;
	using BMTP3.MessageFormatter.Functions;

	public class BuiltInFunctionsUnitTests
	{
		#region TrimFunction Tests

		[Fact]
		public void TrimFunction_LeadingWhitespace()
		{
			var func = new TrimFunction();
			object? result = func.Execute("  hello", new List<object>());

			Assert.Equal("hello", result);
		}

		[Fact]
		public void TrimFunction_TrailingWhitespace()
		{
			var func = new TrimFunction();
			object? result = func.Execute("hello  ", new List<object>());

			Assert.Equal("hello", result);
		}

		[Fact]
		public void TrimFunction_BothWhitespace()
		{
			var func = new TrimFunction();
			object? result = func.Execute("  hello  ", new List<object>());

			Assert.Equal("hello", result);
		}

		[Fact]
		public void TrimFunction_NoWhitespace()
		{
			var func = new TrimFunction();
			object? result = func.Execute("hello", new List<object>());

			Assert.Equal("hello", result);
		}

		[Fact]
		public void TrimFunction_EmptyString()
		{
			var func = new TrimFunction();
			object? result = func.Execute("", new List<object>());

			Assert.Equal("", result);
		}

		[Fact]
		public void TrimFunction_OnlyWhitespace()
		{
			var func = new TrimFunction();
			object? result = func.Execute("   ", new List<object>());

			Assert.Equal("", result);
		}

		[Fact]
		public void TrimFunction_Null()
		{
			var func = new TrimFunction();
			object? result = func.Execute(null, new List<object>());

			Assert.Null(result);
		}

		[Fact]
		public void TrimFunction_GetResultType()
		{
			var func = new TrimFunction();
			string resultType = func.GetResultType("anything");

			Assert.Equal("string", resultType);
		}

		#endregion

		#region ToUpperFunction Tests

		[Fact]
		public void ToUpperFunction_LowercaseString()
		{
			var func = new ToUpperFunction();
			object? result = func.Execute("hello", new List<object>());

			Assert.Equal("HELLO", result);
		}

		[Fact]
		public void ToUpperFunction_MixedCaseString()
		{
			var func = new ToUpperFunction();
			object? result = func.Execute("HeLLo", new List<object>());

			Assert.Equal("HELLO", result);
		}

		[Fact]
		public void ToUpperFunction_WithNumbers()
		{
			var func = new ToUpperFunction();
			object? result = func.Execute("hello123", new List<object>());

			Assert.Equal("HELLO123", result);
		}

		[Fact]
		public void ToUpperFunction_EmptyString()
		{
			var func = new ToUpperFunction();
			object? result = func.Execute("", new List<object>());

			Assert.Equal("", result);
		}

		[Fact]
		public void ToUpperFunction_Null()
		{
			var func = new ToUpperFunction();
			object? result = func.Execute(null, new List<object>());

			Assert.Null(result);
		}

		#endregion

		#region ToLowerFunction Tests

		[Fact]
		public void ToLowerFunction_UppercaseString()
		{
			var func = new ToLowerFunction();
			object? result = func.Execute("HELLO", new List<object>());

			Assert.Equal("hello", result);
		}

		[Fact]
		public void ToLowerFunction_MixedCaseString()
		{
			var func = new ToLowerFunction();
			object? result = func.Execute("HeLLo", new List<object>());

			Assert.Equal("hello", result);
		}

		[Fact]
		public void ToLowerFunction_WithNumbers()
		{
			var func = new ToLowerFunction();
			object? result = func.Execute("HELLO123", new List<object>());

			Assert.Equal("hello123", result);
		}

		#endregion

		#region SubstringFunction Tests

		[Fact]
		public void SubstringFunction_WithStartIndex()
		{
			var func = new SubstringFunction();
			object? result = func.Execute("hello world", new List<object> { 0 });

			Assert.Equal("hello world", result);
		}

		[Fact]
		public void SubstringFunction_WithStartAndLength()
		{
			var func = new SubstringFunction();
			object? result = func.Execute("hello world", new List<object> { 0, 5 });

			Assert.Equal("hello", result);
		}

		[Fact]
		public void SubstringFunction_PartialString()
		{
			var func = new SubstringFunction();
			object? result = func.Execute("hello world", new List<object> { 6, 5 });

			Assert.Equal("world", result);
		}

		[Fact]
		public void SubstringFunction_LengthExceedsString()
		{
			var func = new SubstringFunction();
			object? result = func.Execute("hello", new List<object> { 1, 100 });

			Assert.Equal("ello", result);
		}

		[Fact]
		public void SubstringFunction_ZeroLength()
		{
			var func = new SubstringFunction();
			object? result = func.Execute("hello", new List<object> { 0, 0 });

			Assert.Equal("", result);
		}

		[Fact]
		public void SubstringFunction_NoArgument_Throws()
		{
			var func = new SubstringFunction();
			Assert.Throws<ArgumentException>(() => func.Execute("hello", new List<object>()));
		}

		#endregion

		#region ReplaceFunction Tests

		[Fact]
		public void ReplaceFunction_SimpleReplace()
		{
			var func = new ReplaceFunction();
			object? result = func.Execute("hello", new List<object> { "l", "L" });

			Assert.Equal("heLLo", result);
		}

		[Fact]
		public void ReplaceFunction_NoMatch()
		{
			var func = new ReplaceFunction();
			object? result = func.Execute("hello", new List<object> { "x", "y" });

			Assert.Equal("hello", result);
		}

		[Fact]
		public void ReplaceFunction_EmptyNewValue()
		{
			var func = new ReplaceFunction();
			object? result = func.Execute("hello", new List<object> { "l", "" });

			Assert.Equal("heo", result);
		}

		[Fact]
		public void ReplaceFunction_MissingArguments_Throws()
		{
			var func = new ReplaceFunction();
			Assert.Throws<ArgumentException>(() => func.Execute("hello", new List<object> { "old" }));
		}

		#endregion

		#region PadLeftFunction Tests

		[Fact]
		public void PadLeftFunction_WithWidth()
		{
			var func = new PadLeftFunction();
			object? result = func.Execute("hi", new List<object> { 10 });

			Assert.Equal("        hi", result);
		}

		[Fact]
		public void PadLeftFunction_WithWidthAndChar()
		{
			var func = new PadLeftFunction();
			object? result = func.Execute("hi", new List<object> { 10, "-" });

			Assert.Equal("--------hi", result);
		}

		[Fact]
		public void PadLeftFunction_StringLongerThanWidth()
		{
			var func = new PadLeftFunction();
			object? result = func.Execute("hello", new List<object> { 3 });

			Assert.Equal("hello", result);
		}

		[Fact]
		public void PadLeftFunction_ZeroWidth()
		{
			var func = new PadLeftFunction();
			object? result = func.Execute("hi", new List<object> { 0 });

			Assert.Equal("hi", result);
		}

		[Fact]
		public void PadLeftFunction_NoArgument_Throws()
		{
			var func = new PadLeftFunction();
			Assert.Throws<ArgumentException>(() => func.Execute("hi", new List<object>()));
		}

		#endregion

		#region PadRightFunction Tests

		[Fact]
		public void PadRightFunction_WithWidth()
		{
			var func = new PadRightFunction();
			object? result = func.Execute("hi", new List<object> { 10 });

			Assert.Equal("hi        ", result);
		}

		[Fact]
		public void PadRightFunction_WithWidthAndChar()
		{
			var func = new PadRightFunction();
			object? result = func.Execute("hi", new List<object> { 10, "*" });

			Assert.Equal("hi********", result);
		}

		[Fact]
		public void PadRightFunction_StringLongerThanWidth()
		{
			var func = new PadRightFunction();
			object? result = func.Execute("hello", new List<object> { 3 });

			Assert.Equal("hello", result);
		}

		#endregion

		#region AbsFunction Tests

		[Fact]
		public void AbsFunction_NegativeInteger()
		{
			var func = new AbsFunction();
			object? result = func.Execute(-42, new List<object>());

			Assert.Equal(42, result);
		}

		[Fact]
		public void AbsFunction_PositiveInteger()
		{
			var func = new AbsFunction();
			object? result = func.Execute(42, new List<object>());

			Assert.Equal(42, result);
		}

		[Fact]
		public void AbsFunction_Zero()
		{
			var func = new AbsFunction();
			object? result = func.Execute(0, new List<object>());

			Assert.Equal(0, result);
		}

		[Fact]
		public void AbsFunction_NegativeDouble()
		{
			var func = new AbsFunction();
			object? result = func.Execute(-42.5, new List<object>());

			Assert.Equal(42.5, result);
		}

		[Fact]
		public void AbsFunction_NegativeLong()
		{
			var func = new AbsFunction();
			object? result = func.Execute(-1000000000L, new List<object>());

			Assert.Equal(1000000000L, result);
		}

		[Fact]
		public void AbsFunction_String_Throws()
		{
			var func = new AbsFunction();
			Assert.Throws<InvalidOperationException>(() => func.Execute("not a number", new List<object>()));
		}

		[Fact]
		public void AbsFunction_Null_ReturnsNull()
		{
			var func = new AbsFunction();
			object? result = func.Execute(null, new List<object>());

			Assert.Null(result);
		}

		#endregion

		#region ToStringFunction Tests

		[Fact]
		public void ToStringFunction_Integer()
		{
			var func = new ToStringFunction();
			object? result = func.Execute(1024, new List<object>());

			Assert.Equal("1024", result);
		}

		[Fact]
		public void ToStringFunction_Double()
		{
			var func = new ToStringFunction();
			object? result = func.Execute(3.14, new List<object>());

			Assert.NotEmpty(result?.ToString() ?? "");
		}

		[Fact]
		public void ToStringFunction_Zero()
		{
			var func = new ToStringFunction();
			object? result = func.Execute(0, new List<object>());

			Assert.Equal("0", result);
		}

		[Fact]
		public void ToStringFunction_String()
		{
			var func = new ToStringFunction();
			object? result = func.Execute("hello", new List<object>());

			Assert.Equal("hello", result);
		}

		[Fact]
		public void ToStringFunction_Null()
		{
			var func = new ToStringFunction();
			object? result = func.Execute(null, new List<object>());

			Assert.Equal("", result);
		}

		#endregion

		#region AddDaysFunction Tests

		[Fact]
		public void AddDaysFunction_PositiveDays()
		{
			var func = new AddDaysFunction();
			DateTime input = new(2025, 4, 17);
			object? result = func.Execute(input, new List<object> { 1 });

			Assert.Equal(new DateTime(2025, 4, 18), result);
		}

		[Fact]
		public void AddDaysFunction_NegativeDays()
		{
			var func = new AddDaysFunction();
			DateTime input = new(2025, 4, 17);
			object? result = func.Execute(input, new List<object> { -1 });

			Assert.Equal(new DateTime(2025, 4, 16), result);
		}

		[Fact]
		public void AddDaysFunction_ZeroDays()
		{
			var func = new AddDaysFunction();
			DateTime input = new(2025, 4, 17);
			object? result = func.Execute(input, new List<object> { 0 });

			Assert.Equal(input, result);
		}

		[Fact]
		public void AddDaysFunction_NoArgument_Throws()
		{
			var func = new AddDaysFunction();
			DateTime input = new(2025, 4, 17);
			Assert.Throws<ArgumentException>(() => func.Execute(input, new List<object>()));
		}

		[Fact]
		public void AddDaysFunction_String_Throws()
		{
			var func = new AddDaysFunction();
			Assert.Throws<InvalidOperationException>(() => func.Execute("not a datetime", new List<object> { 1 }));
		}

		#endregion

		#region AddHoursFunction Tests

		[Fact]
		public void AddHoursFunction_PositiveHours()
		{
			var func = new AddHoursFunction();
			DateTime input = new(2025, 4, 17, 10, 30, 0);
			object? result = func.Execute(input, new List<object> { 1 });

			Assert.Equal(new DateTime(2025, 4, 17, 11, 30, 0), result);
		}

		[Fact]
		public void AddHoursFunction_NegativeHours()
		{
			var func = new AddHoursFunction();
			DateTime input = new(2025, 4, 17, 10, 30, 0);
			object? result = func.Execute(input, new List<object> { -1 });

			Assert.Equal(new DateTime(2025, 4, 17, 9, 30, 0), result);
		}

		[Fact]
		public void AddHoursFunction_ZeroHours()
		{
			var func = new AddHoursFunction();
			DateTime input = new(2025, 4, 17, 10, 30, 0);
			object? result = func.Execute(input, new List<object> { 0 });

			Assert.Equal(input, result);
		}

		[Fact]
		public void AddHoursFunction_LargeHours()
		{
			var func = new AddHoursFunction();
			DateTime input = new(2025, 4, 17, 10, 30, 0);
			object? result = func.Execute(input, new List<object> { 48 });

			Assert.Equal(new DateTime(2025, 4, 19, 10, 30, 0), result);
		}

		#endregion

		#region Function Result Type Tests

		[Fact]
		public void TrimFunction_ResultType_IsString()
		{
			var func = new TrimFunction();
			string resultType = func.GetResultType("anything");

			Assert.Equal("string", resultType);
		}

		[Fact]
		public void ToUpperFunction_ResultType_IsString()
		{
			var func = new ToUpperFunction();
			string resultType = func.GetResultType("anything");

			Assert.Equal("string", resultType);
		}

		[Fact]
		public void AbsFunction_ResultType_IsSameAsInput()
		{
			var func = new AbsFunction();
			string resultType = func.GetResultType("integer");

			Assert.Equal("integer", resultType);
		}

		[Fact]
		public void ToStringFunction_ResultType_IsString()
		{
			var func = new ToStringFunction();
			string resultType = func.GetResultType("anything");

			Assert.Equal("string", resultType);
		}

		[Fact]
		public void AddDaysFunction_ResultType_IsSameAsInput()
		{
			var func = new AddDaysFunction();
			string resultType = func.GetResultType("datetime");

			Assert.Equal("datetime", resultType);
		}

		#endregion
	}
}
