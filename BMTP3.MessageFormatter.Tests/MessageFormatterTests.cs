namespace BMTP3.MessageFormatter.Tests
{
	using Xunit;
	using System.Collections.Generic;

	public class MessageFormatterTests
	{
		private readonly MessageFormatter _formatter = new();

		[Fact]
		public void Format_EmptyTemplate_ReturnsEmpty()
		{
			string result = _formatter.Format("");
			Assert.Equal("", result);
		}

		[Fact]
		public void Format_PlainText_ReturnsText()
		{
			string result = _formatter.Format("Hello World");
			Assert.Equal("Hello World", result);
		}

		[Fact]
		public void Format_NamedPlaceholder_WithArgument_ReplacesPlaceholder()
		{
			string result = _formatter.Format("Hello ${name}", new Dictionary<string, object?> { { "name", "Alice" } });
			Assert.Equal("Hello Alice", result);
		}

		[Fact]
		public void Format_NamedPlaceholder_MissingArgument_Throws()
		{
			Assert.Throws<MissingVariableException>(() =>
				_formatter.Format("Hello ${name}", new Dictionary<string, object?> { })
			);
		}

		[Fact]
		public void Format_IndexedPlaceholder_WithArgument_ReplacesPlaceholder()
		{
			string result = _formatter.Format("Hello #{0}", "Bob");
			Assert.Equal("Hello Bob", result);
		}

		[Fact]
		public void Format_IndexedPlaceholder_OutOfRange_Throws()
		{
			Assert.Throws<MissingVariableException>(() =>
				_formatter.Format("Hello #{5}", "Bob")
			);
		}

		[Fact]
		public void Format_FunctionCall_AppliesFunction()
		{
			string result = _formatter.Format("${text.toUpper()}", new Dictionary<string, object?> { { "text", "hello" } });
			Assert.Equal("HELLO", result);
		}

		[Fact]
		public void Format_ChainedFunctions_AppliesBoth()
		{
			string result = _formatter.Format("${text.trim().toUpper()}", new Dictionary<string, object?> { { "text", "  hello  " } });
			Assert.Equal("HELLO", result);
		}

		[Fact]
		public void Format_UnregisteredFunction_Throws()
		{
			Assert.Throws<FunctionNotRegisteredException>(() =>
				_formatter.Format("${text.unknownFunc()}", new Dictionary<string, object?> { { "text", "hello" } })
			);
		}

		[Fact]
		public void Format_NumberFormat_WithStyle()
		{
			string result = _formatter.Format("#{0, number, currency}", 100);
			Assert.NotEmpty(result);
		}

		[Fact]
		public void Format_DateFormat_WithStyle()
		{
			DateTime date = new(2025, 4, 17);
			string result = _formatter.Format("#{0, date, short}", date);
			Assert.NotEmpty(result);
		}

		[Fact]
		public void Format_EscapeSequence_DoubleBraces_BecomeSingleBrace()
		{
			string result = _formatter.Format("Value: {{count}}");
			Assert.Contains("{", result);
		}

		[Fact]
		public void Format_MultipleReplacements_AllReplaced()
		{
			string result = _formatter.Format("${first} and ${second}", 
				new Dictionary<string, object?> 
				{ 
					{ "first", "Alice" },
					{ "second", "Bob" }
				});
			Assert.Equal("Alice and Bob", result);
		}

		[Fact]
		public void Format_MixedNamedAndIndexed_BothWork()
		{
			string result = _formatter.Format("${name} and #{0}",
				new Dictionary<string, object?> { { "name", "Alice" } },
				"Bob");
			Assert.Equal("Alice and Bob", result);
		}

		[Fact]
		public void Format_NullValue_BecomesEmpty()
		{
			string result = _formatter.Format("Value: ${value}", new Dictionary<string, object?> { { "value", null } });
			Assert.Equal("Value: ", result);
		}


		[Fact]
		public void Format_WithComplexExpression_Works()
		{
			string result = _formatter.Format("Total: ${total.toString()}", 
				new Dictionary<string, object?> { { "total", 42 } });
			Assert.Contains("42", result);
		}

		[Fact]
		public void Format_WithNoArguments_StillWorks()
		{
			string result = _formatter.Format("Static text");
			Assert.Equal("Static text", result);
		}

		[Fact]
		public void Format_MoreThanOnePlaceholder_AllReplaced()
		{
			string result = _formatter.Format("#{0} #{1} #{2}",
				"1", "2", "3");
			Assert.Equal("1 2 3", result);
		}
	}
}
