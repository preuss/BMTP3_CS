using BMTP3.Common.MessageFormatterParser.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Common.MessageFormatterParser {
	// Main MessageFormatter class
	public class MessageFormatter : IMessageFormatter {
		public string Format(string template, Dictionary<string, object> values) {
			throw new NotImplementedException();
			/*
			var lexer = new Lexer(template);
			
			var tokens = lexer.Tokenize();
			var parser = new Parser(tokens);

			// Debug output
			//Console.WriteLine("Tokens: " + string.Join(", ", tokens)); // TODO: Debug output
			System.Diagnostics.Debug.WriteLine($"Template: {template}"); // TODO: Debug output
			System.Diagnostics.Debug.WriteLine("Tokens: " + string.Join(", ", tokens)); // TODO: Debug output

			RootNode ast = parser.Parse();
			var typeChecker = new TypeChecker(values.ToDictionary(kv => kv.Key, kv => kv.Value.GetType()));
			typeChecker.Validate(ast);
			var evaluator = new Evaluator(values);
			return evaluator.Evaluate(ast);
			*/
		}
	}
}
