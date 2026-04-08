using BMTP3.Common.MessageFormatterParser;
using BMTP3.Common.MessageFormatterParser.Nodes;

namespace BMTP3.Common.Tests.MessageFormatterParser;

public class EvaluatorTests
{
	[Fact]
	public void Evaluate_SimpleTextAndPlaceholder_ReturnsCombined()
	{
		RootNode ast = new();
		ast.Children.Add(new TextNode("Hello "));
		ast.Children.Add(new ConcretePlaceholderNode("name", new List<FunctionCallNode>(), null, null));

		Dictionary<string, object> values = new() { { "name", "World" } };
		Evaluator ev = new(values);

		string result = ev.Evaluate(ast);

		Assert.Equal("Hello World", result);
	}

	[Fact]
	public void Evaluate_DatePattern_ReturnsFormattedParts()
	{
		List<AstNode> pattern = new()
			{ new TextNode("yyyy"), new TextNode("-"), new TextNode("MM"), new TextNode("-"), new TextNode("dd") };
		ConcretePlaceholderNode placeholder = new("date", new List<FunctionCallNode>(), pattern, null);
		RootNode ast = new();
		ast.Children.Add(placeholder);

		DateTime dt = new(2021, 7, 9, 13, 5, 2);
		Evaluator ev = new(new Dictionary<string, object> { { "date", dt } });

		string result = ev.Evaluate(ast);

		Assert.Equal("2021-07-09", result);
	}

	[Fact]
	public void Evaluate_ToUpperFunction_Works()
	{
		FunctionCallNode func = new("toUpper", new List<AstNode>());
		ConcretePlaceholderNode placeholder = new("name", new List<FunctionCallNode> { func }, null, null);
		RootNode ast = new();
		ast.Children.Add(placeholder);

		Evaluator ev = new(new Dictionary<string, object> { { "name", "abc" } });

		string result = ev.Evaluate(ast);

		Assert.Equal("ABC", result);
	}

	[Fact]
	public void Evaluate_FormatFunction_Works()
	{
		FunctionCallNode func = new("format", new List<AstNode> { new TextNode("{0:0.00}") });
		ConcretePlaceholderNode placeholder = new("num", new List<FunctionCallNode> { func }, null, null);
		RootNode ast = new();
		ast.Children.Add(placeholder);

		Evaluator ev = new(new Dictionary<string, object> { { "num", 3.14159 } });

		string result = ev.Evaluate(ast);

		Assert.Equal("3.14", result);
	}
}