using BMTP3.Common.MessageFormatterParser;

namespace BMTP3.Common.Tests.MessageFormatterParser
{
	public class EvalNestedAndFormatTests
	{
		[Fact]
		public void Eval_NestedPlaceholdersInBranches_AreResolved()
		{
			var template = "Result: ${x§if,eq0?no ${y}:${y} items}";
			var values = new Dictionary<string, object>
			{
				{ "x", 1.0 },
				{ "y", "many" }
			};

			var result = new MessageFormatter().Format(template, values);
			Assert.Equal("Result: many items", result);
		}

		[Fact]
		public void Function_Format_CompositeAndIFormattable_AreApplied()
		{
			var template = "Pi: ${pi.format('{0:0.00}') }";
			var values = new Dictionary<string, object>
			{
				{ "pi", 3.14159 }
			};

			var result = new MessageFormatter().Format(template, values);
			Assert.Equal("Pi: 3.14", result);
		}
	}
}
