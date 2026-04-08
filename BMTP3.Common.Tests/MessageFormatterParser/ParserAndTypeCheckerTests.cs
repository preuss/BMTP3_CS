using BMTP3.Common.MessageFormatterParser;
using BMTP3.Common.MessageFormatterParser.Nodes;

namespace BMTP3.Common.Tests.MessageFormatterParser;

public class ParserAndTypeCheckerTests
{
	[Fact]
	public void Parser_ParsesSimplePlaceholder()
	{
		Lexer2 lexer = new("${filename}");
		Parser2 parser = new(lexer);
		RootNode ast = parser.Parse();

		Assert.Single(ast.Children);
		ConcretePlaceholderNode ph = Assert.IsType<ConcretePlaceholderNode>(ast.Children[0]);
		Assert.Equal("filename", ph.NameOrIndex);
	}

	[Fact]
	public void Parser_ParsesIfCondition()
	{
		// Use the section marker '§' which the lexer/parser expect for eval patterns
		string template = "${count§if,eq0?No files:One or more}";
		Lexer2 lexer = new(template);
		Parser2 parser = new(lexer);
		RootNode ast = parser.Parse();

		Assert.Single(ast.Children);
		ConcretePlaceholderNode ph = Assert.IsType<ConcretePlaceholderNode>(ast.Children[0]);
		Assert.NotNull(ph.Condition);
		IfConditionNode? cond = ph.Condition;
		Assert.IsType<IfConditionNode>(cond);

		// True branch should contain the text "No files"
		Assert.NotEmpty(cond.TrueValue);
		TextNode trueNode = Assert.IsType<TextNode>(cond.TrueValue[0]);
		Assert.Equal("No files", trueNode.Value);

		// False branch should contain the text "One or more"
		Assert.NotEmpty(cond.FalseValue);
		TextNode falseNode = Assert.IsType<TextNode>(cond.FalseValue[0]);
		Assert.Equal("One or more", falseNode.Value);
	}

	[Fact]
	public void TypeChecker_ThrowsOnUnknownPlaceholder()
	{
		Lexer2 lexer = new("${unknown}");
		Parser2 parser = new(lexer);
		RootNode ast = parser.Parse();

		TypeChecker checker = new(new Dictionary<string, Type>());
		Assert.Throws<KeyNotFoundException>(() => checker.Validate(ast));
	}

	[Fact]
	public void MessageFormatter_EvaluatesIfCondition()
	{
		string template = "Files: ${count§if,eq0?no files:${other} files}";
		Dictionary<string, object> values = new()
		{
			{ "count", 2.0 },
			{ "other", "two" }
		};

		string result = new MessageFormatter().Format(template, values);

		Assert.Equal("Files: two files", result);
	}
}