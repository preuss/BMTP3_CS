namespace BMTP3.MessageFormatter.Tests.Models
{
	using Xunit;
	using BMTP3.MessageFormatter.Models;

	public class ModelsTests
	{
		#region Token Tests

		[Fact]
		public void Token_Constructor_SetsProperties()
		{
			Token token = new(TokenType.Text, "hello", 0);

			Assert.Equal(TokenType.Text, token.Type);
			Assert.Equal("hello", token.Value);
			Assert.Equal(0, token.Position);
		}

		[Fact]
		public void Token_ToString_FormatsCorrectly()
		{
			Token token = new(TokenType.NamedPlaceholder, "name", 5);
			string result = token.ToString();

			Assert.Contains("NamedPlaceholder", result);
			Assert.Contains("name", result);
			Assert.Contains("5", result);
		}

		#endregion

		#region ParsedExpression Tests

		[Fact]
		public void ParsedExpression_Default_HasEmptyFunctions()
		{
			ParsedExpression expr = new()
			{
				Variable = new NamedVariableReference("test")
			};

			Assert.Empty(expr.Functions);
		}

		[Fact]
		public void ParsedExpression_CanAddFunctions()
		{
			ParsedExpression expr = new()
			{
				Variable = new NamedVariableReference("test")
			};
			expr.Functions.Add(new FunctionCall("trim"));
			expr.Functions.Add(new FunctionCall("toUpper"));

			Assert.Equal(2, expr.Functions.Count);
		}

		[Fact]
		public void ParsedExpression_FormatExpression_SetsFormatProperties()
		{
			ParsedExpression expr = new()
			{
				Variable = new NamedVariableReference("date"),
				ExpressionType = ExpressionType.Format,
				FormatType = "date",
				FormatStyle = "short"
			};

			Assert.Equal(ExpressionType.Format, expr.ExpressionType);
			Assert.Equal("date", expr.FormatType);
			Assert.Equal("short", expr.FormatStyle);
		}

		[Fact]
		public void ParsedExpression_EvalExpression_SetsEvalProperties()
		{
			ParsedExpression expr = new()
			{
				Variable = new NamedVariableReference("count"),
				ExpressionType = ExpressionType.Eval,
				EvalType = "plural",
				EvalPattern = "one: 1 item | other: # items"
			};

			Assert.Equal(ExpressionType.Eval, expr.ExpressionType);
			Assert.Equal("plural", expr.EvalType);
			Assert.Equal("one: 1 item | other: # items", expr.EvalPattern);
		}

		#endregion

		#region NamedVariableReference Tests

		[Fact]
		public void NamedVariableReference_Constructor_SetsName()
		{
			NamedVariableReference varRef = new("userName");

			Assert.Equal("userName", varRef.Name);
		}

		[Fact]
		public void NamedVariableReference_ToString_FormatsCorrectly()
		{
			NamedVariableReference varRef = new("userName");
			string result = varRef.ToString();

			Assert.Equal("${userName}", result);
		}

		[Fact]
		public void NamedVariableReference_Position_CanBeSet()
		{
			NamedVariableReference varRef = new("test") { Position = 10 };

			Assert.Equal(10, varRef.Position);
		}

		#endregion

		#region IndexedVariableReference Tests

		[Fact]
		public void IndexedVariableReference_Constructor_SetsIndex()
		{
			IndexedVariableReference varRef = new(5);

			Assert.Equal(5, varRef.Index);
		}

		[Fact]
		public void IndexedVariableReference_ToString_FormatsCorrectly()
		{
			IndexedVariableReference varRef = new(3);
			string result = varRef.ToString();

			Assert.Equal("#{3}", result);
		}

		[Fact]
		public void IndexedVariableReference_Position_CanBeSet()
		{
			IndexedVariableReference varRef = new(0) { Position = 0 };

			Assert.Equal(0, varRef.Position);
		}

		[Fact]
		public void IndexedVariableReference_WithZeroIndex()
		{
			IndexedVariableReference varRef = new(0);

			Assert.Equal(0, varRef.Index);
			Assert.Equal("#{0}", varRef.ToString());
		}

		#endregion

		#region FunctionCall Tests

		[Fact]
		public void FunctionCall_Constructor_SetsName()
		{
			FunctionCall funcCall = new("trim");

			Assert.Equal("trim", funcCall.Name);
		}

		[Fact]
		public void FunctionCall_Constructor_HasEmptyArguments()
		{
			FunctionCall funcCall = new("toUpper");

			Assert.Empty(funcCall.Arguments);
		}

		[Fact]
		public void FunctionCall_CanAddArguments()
		{
			FunctionCall funcCall = new("substring");
			funcCall.Arguments.Add(0);
			funcCall.Arguments.Add(5);

			Assert.Equal(2, funcCall.Arguments.Count);
			Assert.Equal(0, funcCall.Arguments[0]);
			Assert.Equal(5, funcCall.Arguments[1]);
		}

		[Fact]
		public void FunctionCall_ToString_NoArguments()
		{
			FunctionCall funcCall = new("trim");
			string result = funcCall.ToString();

			Assert.Equal("trim()", result);
		}

		[Fact]
		public void FunctionCall_ToString_WithArguments()
		{
			FunctionCall funcCall = new("substring");
			funcCall.Arguments.Add(0);
			funcCall.Arguments.Add(5);
			string result = funcCall.ToString();

			Assert.Contains("substring", result);
			Assert.Contains("0", result);
			Assert.Contains("5", result);
		}

		#endregion

		#region ExpressionType Tests

		[Fact]
		public void ExpressionType_HasFormatAndEval()
		{
			Assert.True(Enum.IsDefined(typeof(ExpressionType), ExpressionType.Format));
			Assert.True(Enum.IsDefined(typeof(ExpressionType), ExpressionType.Eval));
		}

		#endregion

		#region EvaluationContext Tests

		[Fact]
		public void EvaluationContext_WithNamedArgs_StoresArgs()
		{
			var args = new Dictionary<string, object?> { { "name", "Alice" } };
			EvaluationContext context = new(args);

			var retrieved = context.GetNamedArgument("name");
			Assert.Equal("Alice", retrieved);
		}

		[Fact]
		public void EvaluationContext_WithIndexedArgs_StoresArgs()
		{
			object?[] args = { "Alice", 42 };
			EvaluationContext context = new(args);

			var first = context.GetIndexedArgument(0);
			var second = context.GetIndexedArgument(1);

			Assert.Equal("Alice", first);
			Assert.Equal(42, second);
		}

		[Fact]
		public void EvaluationContext_WithBothArgs_StoresBoth()
		{
			var named = new Dictionary<string, object?> { { "name", "Alice" } };
			object?[] indexed = { 42 };
			EvaluationContext context = new(named, indexed);

			var nameResult = context.GetNamedArgument("name");
			var indexResult = context.GetIndexedArgument(0);

			Assert.Equal("Alice", nameResult);
			Assert.Equal(42, indexResult);
		}

		[Fact]
		public void EvaluationContext_MissingNamedArg_Throws()
		{
			var args = new Dictionary<string, object?> { };
			EvaluationContext context = new(args);

			Assert.Throws<MissingVariableException>(() => context.GetNamedArgument("missing"));
		}

		[Fact]
		public void EvaluationContext_MissingIndexedArg_Throws()
		{
			object?[] args = { };
			EvaluationContext context = new(args);

			Assert.Throws<MissingVariableException>(() => context.GetIndexedArgument(5));
		}

		[Fact]
		public void EvaluationContext_EmptyNamedArgs_Works()
		{
			var empty = new Dictionary<string, object?> { };
			EvaluationContext context = new(empty, new object?[] { });

			Assert.Throws<MissingVariableException>(() => context.GetNamedArgument("any"));
		}

		#endregion
	}
}
