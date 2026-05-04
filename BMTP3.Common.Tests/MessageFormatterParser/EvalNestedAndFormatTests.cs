using ParserFormatter = BMTP3.Common.MessageFormatterParser.MessageFormatter;
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

		string result = new ParserFormatter().Format(template, values);
		Assert.Equal("Result: many items", result);
	}

	[Fact]
	public void Functions_CanBeChained_BeforeFormatting()
	{
		string template = "${name.trim().toUpper()}";
		Dictionary<string, object> values = new()
		{
			{ "name", "  john  " }
		};

		string result = new ParserFormatter().Format(template, values);
		Assert.Equal("JOHN", result);
	}

	[Fact]
	public void Format_Number_WithStyle_IsApplied()
	{
		string template = "${value, number, integer}";
		Dictionary<string, object> values = new()
		{
			{ "value", 1234.567 }
		};

		string result = new ParserFormatter().Format(template, values);
		Assert.Equal("1235", result);
	}

	[Fact]
	public void Format_DateTime_WithCustomPattern_IsApplied()
	{
		string template = "${created, datetime : YYYY-MM-DD hh:mm:ss}";
		Dictionary<string, object> values = new()
		{
			{ "created", new DateTime(2025, 4, 17, 8, 5, 3) }
		};

		string result = new ParserFormatter().Format(template, values);
		Assert.Equal("2025-04-17 08:05:03", result);
	}

	[Fact]
	public void Eval_If_InList_IsResolved()
	{
		string template = "${status § if, in(1,2,3) ? active : inactive}";
		Dictionary<string, object> values = new()
		{
			{ "status", 2 }
		};

		string result = new ParserFormatter().Format(template, values);
		Assert.Equal("active", result);
	}

	[Fact]
	public void Eval_Plural_IsResolved()
	{
		string template = "${n § plural, 0 # no items | 1 # one item | other # ${n} items}";
		Dictionary<string, object> values = new()
		{
			{ "n", 5 }
		};

		string result = new ParserFormatter().Format(template, values);
		Assert.Equal("5 items", result);
	}

	[Fact]
	public void Eval_Select_IsResolved()
	{
		string template = "${gender § select, male # he | female # she | other # they}";
		Dictionary<string, object> values = new()
		{
			{ "gender", "female" }
		};

		string result = new ParserFormatter().Format(template, values);
		Assert.Equal("she", result);
	}
}