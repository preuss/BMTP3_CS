namespace BMTP3.MessageFormatter.Tests.Core
{
	using Xunit;
	using BMTP3.MessageFormatter;

	public class StringTextAndEscapeTests
	{
		private readonly MessageFormatter _formatter = new();

		#region Text Before and After Variable Tests

		[Fact]
		public void Format_TextBefore_Variable()
		{
			string result = _formatter.Format("User: ${name}", new Dictionary<string, object?> { { "name", "Alice" } });
			Assert.Equal("User: Alice", result);
		}

		[Fact]
		public void Format_TextAfter_Variable()
		{
			string result = _formatter.Format("${name} is here", new Dictionary<string, object?> { { "name", "Bob" } });
			Assert.Equal("Bob is here", result);
		}

		[Fact]
		public void Format_TextBefore_And_After_Variable()
		{
			string result = _formatter.Format("Hello ${name}!", new Dictionary<string, object?> { { "name", "Charlie" } });
			Assert.Equal("Hello Charlie!", result);
		}

		[Fact]
		public void Format_Multiple_Variables_With_Text()
		{
			string result = _formatter.Format("${first} and ${second} are friends",
				new Dictionary<string, object?> 
				{ 
					{ "first", "Alice" },
					{ "second", "Bob" }
				});
			Assert.Equal("Alice and Bob are friends", result);
		}

		[Fact]
		public void Format_Variable_With_Punctuation()
		{
			string result = _formatter.Format("The answer is ${answer}.", new Dictionary<string, object?> { { "answer", 42 } });
			Assert.Equal("The answer is 42.", result);
		}

		[Fact]
		public void Format_Variable_In_Sentence()
		{
			string result = _formatter.Format("${count} files were created today.", new Dictionary<string, object?> { { "count", 5 } });
			Assert.Equal("5 files were created today.", result);
		}

		[Fact]
		public void Format_Variable_Between_Colons()
		{
			string result = _formatter.Format("Status: ${status} : Complete", new Dictionary<string, object?> { { "status", "Active" } });
			Assert.Equal("Status: Active : Complete", result);
		}

		[Fact]
		public void Format_Variable_In_JSON_Like_Format()
		{
			string result = _formatter.Format("{ name: ${name}, age: ${age} }",
				new Dictionary<string, object?> 
				{ 
					{ "name", "Alice" },
					{ "age", 30 }
				});
			Assert.Equal("{ name: Alice, age: 30 }", result);
		}

		[Fact]
		public void Format_Variable_In_URL()
		{
			string result = _formatter.Format("https://example.com/users/${id}", new Dictionary<string, object?> { { "id", 123 } });
			Assert.Equal("https://example.com/users/123", result);
		}

		[Fact]
		public void Format_Variable_With_Parens()
		{
			string result = _formatter.Format("Function call: func(${arg})", new Dictionary<string, object?> { { "arg", "value" } });
			Assert.Equal("Function call: func(value)", result);
		}

		[Fact]
		public void Format_Indexed_Variable_With_Text()
		{
			string result = _formatter.Format("Item number #{0}: #{1}", "first", "second");
			Assert.Equal("Item number first: second", result);
		}

		[Fact]
		public void Format_Multiple_Text_Segments()
		{
			string result = _formatter.Format("Start #{0} middle #{1} end",
				new object?[] { "first", "second" });
			Assert.Equal("Start first middle second end", result);
		}

		[Fact]
		public void Format_Variable_With_Square_Brackets()
		{
			string result = _formatter.Format("Array[${index}] = ${value}",
				new Dictionary<string, object?> 
				{ 
					{ "index", 0 },
					{ "value", "item" }
				});
			Assert.Equal("Array[0] = item", result);
		}

		#endregion

		#region Escape Sequence Tests (in custom patterns only)

		[Fact]
		public void Format_Escape_ClosingBrace_In_DatePattern()
		{
			DateTime date = new(2025, 4, 17);
			string result = _formatter.Format("Date: #{0, datetime : YYYY}}MM}}", date);
			Assert.Equal("Date: 2025}04}", result);
		}

		[Fact]
		public void Format_Escape_OpeningBrace_In_DatePattern()
		{
			DateTime date = new(2025, 4, 17);
			string result = _formatter.Format("Month: #{0, date : {{MM}}", date);
			Assert.Equal("Month: {04}", result);
		}

		[Fact]
		public void Format_Escape_MixedBraces_In_DatePattern()
		{
			DateTime date = new(2025, 4, 17);
			string result = _formatter.Format("Date: #{0, date : {{DD}}MM}}", date);
			Assert.Equal("Date: {17}04}", result);
		}

		[Fact]
		public void Format_Escape_In_TimePattern()
		{
			DateTime time = new(2025, 4, 17, 14, 30, 45);
			string result = _formatter.Format("Time: #{0, time : hh}}mm}}ss}}", time);
			Assert.Equal("Time: 14}30}45}", result);
		}

		[Fact]
		public void Format_Escape_Multiple_In_Pattern()
		{
			DateTime date = new(2025, 4, 17);
			string result = _formatter.Format("Date: #{0, date : {{YYYY}}-{{MM}}-{{DD}}", date);
			Assert.Equal("Date: {2025}-{04}-{17}", result);
		}

		#endregion

		#region Combined: Text and Escape Tests

		[Fact]
		public void Format_Text_Variable_With_Escaped_Pattern()
		{
			DateTime date = new(2025, 4, 17);
			string result = _formatter.Format("Created: #{0, date : {{DD}}/{{MM}}/YYYY}}",
				new object?[] { date });
			Assert.Equal("Created: {17}/{04}/2025}", result);
		}

		[Fact]
		public void Format_Multiple_Variables_With_Text_And_Escape()
		{
			DateTime date1 = new(2025, 4, 17);
			string result = _formatter.Format("From #{0, date : DD}}MM}}, ending now",
				date1);
			Assert.Equal("From 17}04}, ending now", result);
		}

		[Fact]
		public void Format_Variable_With_Function_And_Escape_Pattern()
		{
			DateTime date = new(2025, 4, 17, 14, 30, 45);
			string result = _formatter.Format("Timestamp: #{0, time : {{hh}}:mm:ss}}",
				date);
			Assert.Equal("Timestamp: {14}:30:45}", result);
		}

		#endregion
	}
}
