namespace BMTP3.MessageFormatter.Tests
{
	using Xunit;
	using BMTP3.MessageFormatter.Core;
	using BMTP3.MessageFormatter.Models;

	public class ParserTests
	{
		[Fact]
		public void Parse_SimpleText_ReturnsText()
		{
			MessageParser parser = new();
			List<object> result = parser.Parse("Hello World");

			Assert.Single(result);
			Assert.Equal("Hello World", result[0]);
		}

		[Fact]
		public void Parse_NamedPlaceholder_ReturnsParsedExpression()
		{
			MessageParser parser = new();
			List<object> result = parser.Parse("Hello ${name}");

			Assert.Equal(2, result.Count);
			Assert.Equal("Hello ", result[0]);
			Assert.IsType<ParsedExpression>(result[1]);

			ParsedExpression expr = (ParsedExpression)result[1];
			Assert.IsType<NamedVariableReference>(expr.Variable);
			Assert.Equal("name", ((NamedVariableReference)expr.Variable).Name);
		}

		[Fact]
		public void Parse_IndexedPlaceholder_ReturnsParsedExpression()
		{
			MessageParser parser = new();
			List<object> result = parser.Parse("File #{0}");

			Assert.Equal(2, result.Count);
			Assert.Equal("File ", result[0]);
			Assert.IsType<ParsedExpression>(result[1]);

			ParsedExpression expr = (ParsedExpression)result[1];
			Assert.IsType<IndexedVariableReference>(expr.Variable);
			Assert.Equal(0, ((IndexedVariableReference)expr.Variable).Index);
		}

		[Fact]
		public void Parse_MultipleExpressions_ReturnsAll()
		{
			MessageParser parser = new();
			List<object> result = parser.Parse("User ${name} has #{0} files");

			Assert.Equal(5, result.Count);
			Assert.Equal("User ", result[0]);
			Assert.IsType<ParsedExpression>(result[1]);
			Assert.Equal(" has ", result[2]);
			Assert.IsType<ParsedExpression>(result[3]);
			Assert.Equal(" files", result[4]);
		}

		[Fact]
		public void Parse_PlaceholderWithFunction_ParsesFunctionCall()
		{
			MessageParser parser = new();
			List<object> result = parser.Parse("${name.toUpper()}");

			ParsedExpression expr = (ParsedExpression)result[0];
			Assert.Single(expr.Functions);
			Assert.Equal("toUpper", expr.Functions[0].Name);
		}

		[Fact]
		public void Parse_PlaceholderWithFormat_ParsesFormatType()
		{
			MessageParser parser = new();
			List<object> result = parser.Parse("${amount, number}");

			ParsedExpression expr = (ParsedExpression)result[0];
			Assert.Equal("number", expr.FormatType);
			Assert.Equal(ExpressionType.Format, expr.ExpressionType);
		}

		[Fact]
		public void Parse_PlaceholderWithFormatAndStyle_ParsesBoth()
		{
			MessageParser parser = new();
			List<object> result = parser.Parse("${amount, number, currency}");

			ParsedExpression expr = (ParsedExpression)result[0];
			Assert.Equal("number", expr.FormatType);
			Assert.Equal("currency", expr.FormatStyle);
		}

		[Fact]
		public void Parse_PlaceholderWithCustomPattern_ParsesPattern()
		{
			MessageParser parser = new();
			List<object> result = parser.Parse("${date, datetime : YYYY-MM-DD}");

			ParsedExpression expr = (ParsedExpression)result[0];
			Assert.Equal("datetime", expr.FormatType);
			Assert.NotNull(expr.CustomPattern);
			Assert.Contains("YYYY-MM-DD", expr.CustomPattern);
		}

		[Fact]
		public void Parse_PlaceholderWithEval_ParsesEvalExpression()
		{
			MessageParser parser = new();
			List<object> result = parser.Parse("${count § if, eq0 ? zero : many}");

			ParsedExpression expr = (ParsedExpression)result[0];
			Assert.Equal(ExpressionType.Eval, expr.ExpressionType);
			Assert.Equal("if", expr.EvalType);
			Assert.NotNull(expr.EvalPattern);
		}
	}
}
