namespace BMTP3.MessageFormatter.Core
{
	using Models;
	using Abstractions;
	using System.Text;

	/// <summary>
	/// Evaluates parsed expressions with variable arguments.
	/// </summary>
	public class MessageEvaluator : IMessageEvaluator
	{
		private readonly IFormatTypeRegistry _formatTypeRegistry;
		private readonly IFunctionRegistry _functionRegistry;

		public MessageEvaluator(IFormatTypeRegistry formatTypeRegistry, IFunctionRegistry functionRegistry)
		{
			_formatTypeRegistry = formatTypeRegistry ?? throw new ArgumentNullException(nameof(formatTypeRegistry));
			_functionRegistry = functionRegistry ?? throw new ArgumentNullException(nameof(functionRegistry));
		}

		public string Evaluate(ParsedExpression expression, Dictionary<string, object?> namedArgs)
		{
			EvaluationContext context = new(namedArgs);
			return Evaluate(expression, context);
		}

		public string Evaluate(ParsedExpression expression, params object?[] indexedArgs)
		{
			EvaluationContext context = new(indexedArgs);
			return Evaluate(expression, context);
		}

		public string Evaluate(ParsedExpression expression, Dictionary<string, object?> namedArgs, params object?[] indexedArgs)
		{
			EvaluationContext context = new(namedArgs, indexedArgs);
			return Evaluate(expression, context);
		}

		public string Evaluate(ParsedExpression expression, EvaluationContext context)
		{
			object? value = ResolveVariable(expression.Variable, context);
			string currentType = GetTypeName(value);

			foreach (FunctionCall funcCall in expression.Functions)
			{
				value = ExecuteFunction(funcCall, value, currentType);
				currentType = GetTypeName(value);
			}

			if (expression.ExpressionType == ExpressionType.Format)
			{
				return EvaluateFormatExpression(value, currentType, expression, context);
			}

			if (expression.ExpressionType == ExpressionType.Eval)
			{
				return EvaluateEvalExpression(value, currentType, expression, context);
			}

			throw new MessageEvaluationException("Unknown expression type");
		}

		private object? ResolveVariable(VariableReference variable, EvaluationContext context)
		{
			if (variable is NamedVariableReference named)
			{
				return context.GetNamedArgument(named.Name);
			}

			if (variable is IndexedVariableReference indexed)
			{
				return context.GetIndexedArgument(indexed.Index);
			}

			throw new MessageEvaluationException("Unknown variable reference type");
		}

		private object? ExecuteFunction(FunctionCall call, object? value, string currentType)
		{
			if (!_functionRegistry.TryGetFunction(currentType, call.Name, out IFunction? function))
			{
				throw new FunctionNotRegisteredException(call.Name, currentType);
			}

			return function!.Execute(value, call.Arguments);
		}

		private string EvaluateFormatExpression(object? value, string currentType, ParsedExpression expr, EvaluationContext context)
		{
			if (!string.IsNullOrEmpty(expr.FormatType))
			{
				IFormatType formatType = _formatTypeRegistry.GetFormatType(expr.FormatType);
				formatType.AssertCompatible(value);

				if (!string.IsNullOrEmpty(expr.FormatStyle))
				{
					return formatType.FormatWithStyle(value, expr.FormatStyle);
				}

				if (!string.IsNullOrEmpty(expr.CustomPattern))
				{
					string protectedPattern = ProtectNestedPlaceholders(expr.CustomPattern, out Dictionary<string, string> placeholders);
					string formatted = formatType.FormatWithPattern(value, protectedPattern);
					formatted = RestoreProtectedPlaceholders(formatted, placeholders);
					if (formatted.Contains("${", StringComparison.Ordinal) || formatted.Contains("#{", StringComparison.Ordinal))
					{
						return EvaluateNestedPlaceholders(formatted, context);
					}

					return formatted;
				}

				return formatType.FormatDefault(value);
			}

			return value?.ToString() ?? string.Empty;
		}

		private string EvaluateEvalExpression(object? value, string currentType, ParsedExpression expr, EvaluationContext context)
		{
			string evalType = expr.EvalType?.ToLowerInvariant() ?? "if";

			if (evalType == "if")
			{
				if (!IsNumeric(value))
				{
					throw new MessageEvaluationException("'if' requires a numeric value");
				}

				return EvaluateIfExpression(value, expr.EvalPattern!, context);
			}

			if (evalType == "plural")
			{
				if (!IsNumeric(value))
				{
					throw new MessageEvaluationException("'plural' requires a numeric value");
				}

				return EvaluatePluralExpression(value, expr.EvalPattern!, context);
			}

			if (evalType == "select")
			{
				if (value is not string)
				{
					throw new MessageEvaluationException("'select' requires a string value");
				}

				return EvaluateSelectExpression(value, expr.EvalPattern!, context);
			}

			throw new MessageEvaluationException($"EvalType '{evalType}' is not recognized");
		}

		private string EvaluateIfExpression(object? value, string pattern, EvaluationContext context)
		{
			int questionPos = pattern.IndexOf('?');
			int colonPos = pattern.LastIndexOf(':');

			if (questionPos < 0 || colonPos < 0 || questionPos > colonPos)
			{
				throw new MessageEvaluationException("Invalid if pattern syntax");
			}

			string condition = pattern[..questionPos].Trim();
			string trueValue = pattern[(questionPos + 1)..colonPos].Trim();
			string falseValue = pattern[(colonPos + 1)..].Trim();

			bool conditionMet = EvaluateCondition(value, condition);
			string selectedValue = conditionMet ? trueValue : falseValue;
			return FinalizeEvalOutput(selectedValue, context);
		}

		private string EvaluatePluralExpression(object? value, string pattern, EvaluationContext context)
		{
			double numValue = Convert.ToDouble(value);
			List<string> rules = SplitTopLevel(pattern, '|');
			string? fallbackValue = null;

			for (int i = 0; i < rules.Count; i++)
			{
				string rule = rules[i];
				int hashPos = IndexOfTopLevel(rule, '#');
				if (hashPos < 0)
				{
					continue;
				}

				string ruleType = rule[..hashPos].Trim();
				string ruleValue = rule[(hashPos + 1)..].Trim();
				if (i == rules.Count - 1)
				{
					fallbackValue = ruleValue;
					continue;
				}

				if (MatchesPluralRule(numValue, ruleType))
				{
					return FinalizeEvalOutput(ruleValue, context);
				}
			}

			if (fallbackValue != null)
			{
				return FinalizeEvalOutput(fallbackValue, context);
			}

			throw new MessageEvaluationException($"No matching plural rule for value {numValue} and no fallback entry");
		}

		private string EvaluateSelectExpression(object? value, string pattern, EvaluationContext context)
		{
			string stringValue = (string)value!;
			List<string> rules = SplitTopLevel(pattern, '|');
			string? fallbackValue = null;

			for (int i = 0; i < rules.Count; i++)
			{
				string rule = rules[i];
				int hashPos = IndexOfTopLevel(rule, '#');
				if (hashPos < 0)
				{
					continue;
				}

				string category = rule[..hashPos].Trim();
				string categoryValue = rule[(hashPos + 1)..].Trim();
				if (i == rules.Count - 1)
				{
					fallbackValue = categoryValue;
				}

				if (category == stringValue)
				{
					return FinalizeEvalOutput(categoryValue, context);
				}
			}

			if (fallbackValue != null)
			{
				return FinalizeEvalOutput(fallbackValue, context);
			}

			throw new MessageEvaluationException($"No matching select rule for value '{stringValue}' and no fallback entry");
		}

		private bool EvaluateCondition(object? value, string condition)
		{
			double numericValue = Convert.ToDouble(value);

			if (condition.StartsWith("eq", StringComparison.Ordinal))
			{
				double expected = double.Parse(condition[2..], System.Globalization.CultureInfo.InvariantCulture);
				return numericValue == expected;
			}
			if (condition.StartsWith("ne", StringComparison.Ordinal))
			{
				double expected = double.Parse(condition[2..], System.Globalization.CultureInfo.InvariantCulture);
				return numericValue != expected;
			}
			if (condition.StartsWith("gte", StringComparison.Ordinal))
			{
				double expected = double.Parse(condition[3..], System.Globalization.CultureInfo.InvariantCulture);
				return numericValue >= expected;
			}
			if (condition.StartsWith("gt", StringComparison.Ordinal))
			{
				double expected = double.Parse(condition[2..], System.Globalization.CultureInfo.InvariantCulture);
				return numericValue > expected;
			}
			if (condition.StartsWith("lte", StringComparison.Ordinal))
			{
				double expected = double.Parse(condition[3..], System.Globalization.CultureInfo.InvariantCulture);
				return numericValue <= expected;
			}
			if (condition.StartsWith("lt", StringComparison.Ordinal))
			{
				double expected = double.Parse(condition[2..], System.Globalization.CultureInfo.InvariantCulture);
				return numericValue < expected;
			}
			if (condition.StartsWith("in(", StringComparison.Ordinal))
			{
				int closePos = condition.LastIndexOf(')');
				string listStr = condition[3..closePos];
				double[] list = listStr.Split(',').Select(static x => double.Parse(x.Trim(), System.Globalization.CultureInfo.InvariantCulture)).ToArray();
				return list.Contains(numericValue);
			}
			if (condition.StartsWith("nin(", StringComparison.Ordinal))
			{
				int closePos = condition.LastIndexOf(')');
				string listStr = condition[4..closePos];
				double[] list = listStr.Split(',').Select(static x => double.Parse(x.Trim(), System.Globalization.CultureInfo.InvariantCulture)).ToArray();
				return !list.Contains(numericValue);
			}

			throw new MessageEvaluationException($"Unknown condition: {condition}");
		}

		private bool MatchesPluralRule(double value, string rule)
		{
			if (int.TryParse(rule, out int exactValue))
			{
				return (int)value == exactValue;
			}

			if (rule == "zero")
			{
				return value == 0;
			}

			if (rule == "one")
			{
				return value == 1;
			}

			if (rule == "few")
			{
				return value == 2 || value == 3 || value == 4;
			}

			if (rule == "many")
			{
				return value >= 5;
			}

			if ((rule.StartsWith('[') || rule.StartsWith(']')) && (rule.EndsWith(']') || rule.EndsWith('[')))
			{
				char startBracket = rule[0];
				char endBracket = rule[^1];
				string range = rule[1..^1];

				int semiPos = range.IndexOf(';');
				if (semiPos > 0)
				{
					string startStr = range[..semiPos].Trim();
					string endStr = range[(semiPos + 1)..].Trim();

					if (double.TryParse(startStr, out double rangeStart) && double.TryParse(endStr, out double rangeEnd))
					{
						bool startInclusive = startBracket == '[';
						bool endInclusive = endBracket == ']';

						bool startOk = startInclusive ? value >= rangeStart : value > rangeStart;
						bool endOk = endInclusive ? value <= rangeEnd : value < rangeEnd;

						return startOk && endOk;
					}
				}
			}

			return false;
		}

		private static bool IsNumeric(object? value)
		{
			return value is int || value is long || value is double || value is decimal || value is float || value is short || value is byte;
		}

		private string FinalizeEvalOutput(string value, EvaluationContext context)
		{
			if (value.Contains("${", StringComparison.Ordinal) || value.Contains("#{", StringComparison.Ordinal))
			{
				return EvaluateNestedPlaceholders(value, context);
			}

			return value.Replace("}}", "}", StringComparison.Ordinal).Replace("{{", "{", StringComparison.Ordinal);
		}

		private string EvaluateNestedPlaceholders(string text, EvaluationContext context)
		{
			const int maxDepth = 10;
			return EvaluateNestedPlaceholdersInternal(text, context, 0, maxDepth);
		}

		private string EvaluateNestedPlaceholdersInternal(string text, EvaluationContext context, int depth, int maxDepth)
		{
			if (depth > maxDepth)
			{
				throw new MessageEvaluationException("Maximum nesting depth exceeded");
			}

			text = text.Replace("}}", "§RBRACE§").Replace("{{", "§LBRACE§");

			MessageParser parser = new();
			List<object> parsed = parser.Parse(text);

			StringBuilder sb = new();
			foreach (object item in parsed)
			{
				if (item is string str)
				{
					str = str.Replace("§RBRACE§", "}").Replace("§LBRACE§", "{");
					sb.Append(str);
				}
				else if (item is ParsedExpression expr)
				{
					sb.Append(Evaluate(expr, context));
				}
			}

			return sb.ToString();
		}

		private string GetTypeName(object? value)
		{
			if (value == null)
			{
				return "null";
			}

			if (value is string)
			{
				return "string";
			}

			if (value is int or long)
			{
				return "integer";
			}

			if (value is double or float or decimal)
			{
				return "number";
			}

			if (value is DateTime dt)
			{
				return dt.TimeOfDay == TimeSpan.Zero ? "date" : "datetime";
			}

			if (value is DateOnly)
			{
				return "date";
			}

			if (value is TimeOnly)
			{
				return "time";
			}

			return value.GetType().Name.ToLowerInvariant();
		}

		private static List<string> SplitTopLevel(string input, char separator)
		{
			List<string> parts = new();
			StringBuilder current = new();
			int nestedPlaceholderDepth = 0;

			for (int i = 0; i < input.Length; i++)
			{
				char ch = input[i];

				if ((ch == '$' || ch == '#') && i + 1 < input.Length && input[i + 1] == '{')
				{
					nestedPlaceholderDepth++;
					current.Append(ch);
					current.Append('{');
					i++;
					continue;
				}

				if (ch == '{' && i + 1 < input.Length && input[i + 1] == '{')
				{
					current.Append("{{");
					i++;
					continue;
				}

				if (ch == '}' && i + 1 < input.Length && input[i + 1] == '}')
				{
					current.Append("}}");
					i++;
					continue;
				}

				if (ch == '}' && nestedPlaceholderDepth > 0)
				{
					nestedPlaceholderDepth--;
					current.Append(ch);
					continue;
				}

				if (ch == separator && nestedPlaceholderDepth == 0)
				{
					string part = current.ToString().Trim();
					if (part.Length > 0)
					{
						parts.Add(part);
					}

					current.Clear();
					continue;
				}

				current.Append(ch);
			}

			string last = current.ToString().Trim();
			if (last.Length > 0)
			{
				parts.Add(last);
			}

			return parts;
		}

		private static int IndexOfTopLevel(string input, char target)
		{
			int nestedPlaceholderDepth = 0;

			for (int i = 0; i < input.Length; i++)
			{
				char ch = input[i];

				if ((ch == '$' || ch == '#') && i + 1 < input.Length && input[i + 1] == '{')
				{
					nestedPlaceholderDepth++;
					i++;
					continue;
				}

				if (ch == '{' && i + 1 < input.Length && input[i + 1] == '{')
				{
					i++;
					continue;
				}

				if (ch == '}' && i + 1 < input.Length && input[i + 1] == '}')
				{
					i++;
					continue;
				}

				if (ch == '}' && nestedPlaceholderDepth > 0)
				{
					nestedPlaceholderDepth--;
					continue;
				}

				if (ch == target && nestedPlaceholderDepth == 0)
				{
					return i;
				}
			}

			return -1;
		}

		private static string ProtectNestedPlaceholders(string pattern, out Dictionary<string, string> placeholders)
		{
			placeholders = new Dictionary<string, string>();
			StringBuilder sb = new();
			int i = 0;
			int index = 0;

			while (i < pattern.Length)
			{
				if ((pattern[i] == '$' || pattern[i] == '#') && i + 1 < pattern.Length && pattern[i + 1] == '{')
				{
					int start = i;
					i += 2;
					int depth = 1;
					while (i < pattern.Length && depth > 0)
					{
						if ((pattern[i] == '$' || pattern[i] == '#') && i + 1 < pattern.Length && pattern[i + 1] == '{')
						{
							depth++;
							i += 2;
							continue;
						}

						if (pattern[i] == '}')
						{
							depth--;
						}

						i++;
					}

					string placeholder = pattern[start..i];
					string key = $"§PH{index++}§";
					placeholders[key] = placeholder;
					sb.Append(key);
					continue;
				}

				sb.Append(pattern[i]);
				i++;
			}

			return sb.ToString();
		}

		private static string RestoreProtectedPlaceholders(string value, Dictionary<string, string> placeholders)
		{
			foreach (KeyValuePair<string, string> pair in placeholders)
			{
				value = value.Replace(pair.Key, pair.Value, StringComparison.Ordinal);
			}

			return value;
		}
	}
}
