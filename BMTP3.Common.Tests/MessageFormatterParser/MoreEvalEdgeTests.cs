using ParserFormatter = BMTP3.Common.MessageFormatterParser.MessageFormatter;
using BMTP3.Common.MessageFormatterParser;

namespace BMTP3.Common.Tests.MessageFormatterParser;

public class MoreEvalEdgeTests
{
	[Fact]
	public void Eval_IfBranch_WithNestedPlaceholderAndSpacing()
	{
		string template = "Items: ${count§if,eq0?no items:${name} items}";
		Dictionary<string, object> values = new()
		{
			{ "count", 2.0 },
			{ "name", "several" }
		};

		string result = new ParserFormatter().Format(template, values);
		Assert.Equal("Items: several items", result);
	}

	[Fact]
	public void Eval_IfBranch_ZeroCase_ResolvesZero()
	{
		string template = "Count: ${count§if,eq0?none:some}";
		Dictionary<string, object> values = new()
		{
			{ "count", 0.0 }
		};

		string result = new ParserFormatter().Format(template, values);
		Assert.Equal("Count: none", result);
	}
}