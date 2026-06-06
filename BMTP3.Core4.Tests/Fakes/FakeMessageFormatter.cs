using BMTP3.Common.MessageFormatterParser;

namespace BMTP3.Core4.Tests.Fakes;

internal sealed class FakeMessageFormatter : IMessageFormatter
{
	public string Format(string template, Dictionary<string, object> values)
	{
		string result = template;
		foreach(KeyValuePair<string, object> kvp in values)
		{
			result = result.Replace($"{{{kvp.Key}}}", kvp.Value?.ToString() ?? string.Empty);
		}
		return result;
	}
}
