using BMTP3.Common.MessageFormatterParser;

namespace BMTP3.Common.Tests.MessageFormatterParser;

public class EvalNestedAndFormatTests
{
	[Fact]
	public void Eval_NestedPlaceholdersInBranches_AreResolved()
	{
		string template = "Result: ${x§if,eq0?no ${y}:${y} items}";
		Dictionary<string, object> values = new()
		{
			{ "x", 1.0 },
			{ "y", "many" }
		};

		string result = new MessageFormatter().Format(template, values);
		Assert.Equal("Result: many items", result);
	}

	[Fact]
	public void Function_Format_CompositeAndIFormattable_AreApplied()
	{
		string template = "Pi: ${pi.format('{0:0.00}') }";
		Dictionary<string, object> values = new()
		{
			{ "pi", 3.14159 }
		};

		string result = new MessageFormatter().Format(template, values);
		Assert.Equal("Pi: 3.14", result);
	}
}