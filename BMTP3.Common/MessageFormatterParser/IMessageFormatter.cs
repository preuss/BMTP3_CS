namespace BMTP3.Common.MessageFormatterParser;

public interface IMessageFormatter
{
	public string Format(string template, Dictionary<string, object> values);
}