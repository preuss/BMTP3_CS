using BMTP3.Common.MessageFormatterParser.Nodes;
using System;
using System.Globalization;

namespace BMTP3.Common.MessageFormatterParser {
	public class Evaluator {
		private readonly Dictionary<string, object> values;

		public Evaluator(Dictionary<string, object> values) {
			this.values = values;
		}

		public string Evaluate(RootNode ast) {
			var result = "";
			foreach(var node in ast.Children) {
				if(node is TextNode text)
					result += text.Value;
				else if(node is PlaceholderNode placeholder)
					result += EvaluatePlaceholder(placeholder);
			}
			return result;
		}

		private string EvaluatePlaceholder(PlaceholderNode node) {
			if(!values.ContainsKey(node.NameOrIndex))
				throw new Exception($"Undefined value: {node.NameOrIndex}");

			var value = values[node.NameOrIndex];

			// Apply functions
			foreach(var func in node.Functions) {
				value = ApplyFunction(func.Name, value);
			}

			// Handle if condition
			if(node.Condition != null) {
				bool conditionMet = EvaluateCondition(node.Condition, value);
				var stringToEvaluate = conditionMet ? node.Condition.TrueValue : node.Condition.FalseValue;
				var tempAst = new RootNode();
				tempAst.Children.AddRange(stringToEvaluate!);
				return Evaluate(tempAst)!;
			}

			// Handle pattern
			if(node.Pattern != null) {
				var patternResult = "";
				foreach(var item in node.Pattern) {
					if(item is TextNode text) {
						// Handle date formatting for specific patterns
						DateTime date = DateTime.MinValue;
						bool isDate = false;

						if (value is DateTime dt) 
						{ 
							date = dt; 
							isDate = true; 
						}
						else if (value is string s && DateTime.TryParse(s, out var parsed)) 
						{ 
							date = parsed; 
							isDate = true; 
						}

						if(node.NameOrIndex == "date" && isDate) {
							if(text.Value == "yyyy")
								patternResult += date.Year.ToString("D4");
							else if(text.Value == "MM")
								patternResult += date.Month.ToString("D2");
							else if(text.Value == "dd")
								patternResult += date.Day.ToString("D2");
							else if(text.Value == "HH")
								patternResult += date.ToString("HH"); // Fix: Add time support
							else if(text.Value == "mm")
								patternResult += date.ToString("mm"); // Fix: Add time support
							else if(text.Value == "ss")
								patternResult += date.ToString("ss"); // Fix: Add time support
							else if(text.Value == "EEE")
								patternResult += date.ToString("ddd", CultureInfo.InvariantCulture).Substring(0, 3);
							else
								patternResult += text.Value; // Bevar specialtegn som '/', ':', eller ' '
						} else {
							patternResult += text.Value;
						}
					} else if(item is LiteralNode literal)
						patternResult += literal.Value;
					else if(item is PlaceholderNode nested)
						patternResult += EvaluatePlaceholder(nested);
				}
				return patternResult;
			}

			// Handle type and style
			if(node.Type == "string" && node.Style == "upper")
				return value.ToString().ToUpper();
			if(node.Type == "number" && node.Style == "integer")
				return ((int)(double)value).ToString();

			// Simple placeholder: return value as string
			return value.ToString();
		}

		private bool EvaluateCondition(IfConditionNode condition, object value) {
			/*
			if(condition.ConditionOperator == "eq0")
				return Convert.ToDouble(value) == 0;
			if(condition.ConditionOperator == "gt0")
				return Convert.ToDouble(value) > 0;
				*/
			return false;
		}

		private object ApplyFunction(string func, object value) {
			if(func == "toUpper" && value is string s)
				return s.ToUpper();
			if(func == "toLower" && value is string s2)
				return s2.ToLower();
			if(func == "abs" && value is double d)
				return Math.Abs(d);
			return value;
		}
	}
}
