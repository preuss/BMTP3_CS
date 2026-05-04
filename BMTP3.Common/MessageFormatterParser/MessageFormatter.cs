using FormatterImpl = BMTP3.MessageFormatter.MessageFormatter;

namespace BMTP3.Common.MessageFormatterParser;

public class MessageFormatter : IMessageFormatter
{
	private readonly FormatterImpl _formatter = new();

	public string Format(string template, Dictionary<string, object> values)
	{
		if (string.IsNullOrEmpty(template))
		{
			return string.Empty;
		}

		Dictionary<string, object?> adaptedValues = values.ToDictionary(
			static pair => pair.Key,
			static pair => (object?)pair.Value,
			StringComparer.Ordinal);

		return _formatter.Format(template, adaptedValues);
	}
}