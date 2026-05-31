using BMTP3.Common.MessageFormatterParser;

namespace BMTP3.Core4.Engine.Strategies;

internal sealed class PreparedMessageFormat
{
	private readonly IMessageFormatter _formatter;
	private readonly string _template;
	private readonly Dictionary<string, object> _baseValues;

	public PreparedMessageFormat(
		IMessageFormatter formatter,
		string template,
		Dictionary<string, object> baseValues
	)
	{
		_formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
		_template = template ?? throw new ArgumentNullException(nameof(template));
		_baseValues = baseValues ?? throw new ArgumentNullException(nameof(baseValues));
	}

	public string Format()
		=> _formatter.Format(_template, _baseValues);

	public string Format(Dictionary<string, object> extraValues)
	{
		var merged = new Dictionary<string, object>(_baseValues, StringComparer.Ordinal);
		foreach(KeyValuePair<string, object> kvp in extraValues)
		{
			merged[kvp.Key] = kvp.Value;
		}
		return _formatter.Format(_template, merged);
	}
}
