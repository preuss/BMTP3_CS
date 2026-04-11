using BMTP3.Common.MessageFormatterParser.Nodes;

namespace BMTP3.Common.MessageFormatterParser;

public class MessageFormatter : IMessageFormatter
{
	public string Format(string template, Dictionary<string, object> values)
	{
		if (string.IsNullOrEmpty(template))
		{
			return "";
		}

		// 1. Tokenize
		Lexer2 lexer = new(template);

		// 2. Parse
		Parser2 parser = new(lexer);
		RootNode ast = parser.Parse();

		// 3. Evaluate
		// We skip TypeChecker for now as it relies on reflection logic we haven't verified completely.
		// The Evaluator is robust enough to throw runtime errors if keys miss.
		Evaluator evaluator = new(values);
		return evaluator.Evaluate(ast);
	}
}