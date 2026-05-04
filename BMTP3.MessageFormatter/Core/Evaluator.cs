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
				return EvaluateFormatExpression(value, currentType, expression);
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

		private string EvaluateFormatExpression(object? value, string currentType, ParsedExpression expr)
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
					return formatType.FormatWithPattern(value, expr.CustomPattern);
				}

				return formatType.FormatDefault(value);
			}

			return value?.ToString() ?? string.Empty;
		}

		private string EvaluateEvalExpression(object? value, string currentType, ParsedExpression expr, EvaluationContext context)
		{
			string evalType = expr.EvalType?.ToLowerInvariant() ?? "if";

			return evalType switch
			{
				"if" => EvaluateIfExpression(value, expr.EvalPattern!, context),
				"plural" => EvaluatePluralExpression(value, expr.EvalPattern!, context),
				"select" => EvaluateSelectExpression(value, expr.EvalPattern!, context),
				_ => throw new MessageEvaluationException($"Unknown eval type: {evalType}")
			};
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
			if (selectedValue.Contains("${", StringComparison.Ordinal) || selectedValue.Contains("#{", StringComparison.Ordinal))
			{
				return EvaluateNestedPlaceholders(selectedValue, context);
			}

			return selectedValue;
		}

		private string EvaluatePluralExpression(object? value, string pattern, EvaluationContext context)
		{
			if (!(value is int || value is long || value is double || value is decimal))
			{
				throw new MessageEvaluationException("Plural expressions require numeric values");
			}

			double numValue = Convert.ToDouble(value);
			List<string> rules = pattern.Split('|').Select(static r => r.Trim()).Where(static r => r.Length > 0).ToList();
			string? fallbackValue = null;

			for (int i = 0; i < rules.Count; i++)
			{
				string rule = rules[i];
				int hashPos = rule.IndexOf('#');
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
					if (ruleValue.Contains("${", StringComparison.Ordinal) || ruleValue.Contains("#{", StringComparison.Ordinal))
					{
						return EvaluateNestedPlaceholders(ruleValue, context);
					}

					return ruleValue;
				}
			}

			if (fallbackValue != null)
			{
				if (fallbackValue.Contains("${", StringComparison.Ordinal) || fallbackValue.Contains("#{", StringComparison.Ordinal))
				{
					return EvaluateNestedPlaceholders(fallbackValue, context);
				}

				return fallbackValue;
			}

			throw new MessageEvaluationException($"No matching plural rule for value {numValue} and no fallback entry");
		}

		private string EvaluateSelectExpression(object? value, string pattern, EvaluationContext context)
		{
			string stringValue = value?.ToString() ?? string.Empty;
			List<string> rules = pattern.Split('|').Select(static r => r.Trim()).Where(static r => r.Length > 0).ToList();
			string? fallbackValue = null;

			for (int i = 0; i < rules.Count; i++)
			{
				string rule = rules[i];
				int hashPos = rule.IndexOf('#');
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
					if (categoryValue.Contains("${", StringComparison.Ordinal) || categoryValue.Contains("#{", StringComparison.Ordinal))
					{
						return EvaluateNestedPlaceholders(categoryValue, context);
					}

					return categoryValue;
				}
			}

			if (fallbackValue != null)
			{
				if (fallbackValue.Contains("${", StringComparison.Ordinal) || fallbackValue.Contains("#{", StringComparison.Ordinal))
				{
					return EvaluateNestedPlaceholders(fallbackValue, context);
				}

				return fallbackValue;
			}

			throw new MessageEvaluationException($"No matching select rule for value '{stringValue}' and no fallback entry");
		}

		private bool EvaluateCondition(object? value, string condition)
		{
			if (condition.StartsWith("eq", StringComparison.Ordinal))
			{
				int expected = int.Parse(condition[2..]);
				return Convert.ToInt32(value) == expected;
			}
			else if (condition.StartsWith("ne", StringComparison.Ordinal))
			{
				int expected = int.Parse(condition[2..]);
				return Convert.ToInt32(value) != expected;
			}
			else if (condition.StartsWith("gte", StringComparison.Ordinal))
			{
				int expected = int.Parse(condition[3..]);
				return Convert.ToInt32(value) >= expected;
			}
			else if (condition.StartsWith("gt", StringComparison.Ordinal))
			{
				int expected = int.Parse(condition[2..]);
				return Convert.ToInt32(value) > expected;
			}
			else if (condition.StartsWith("lte", StringComparison.Ordinal))
			{
				int expected = int.Parse(condition[3..]);
				return Convert.ToInt32(value) <= expected;
			}
			else if (condition.StartsWith("lt", StringComparison.Ordinal))
			{
				int expected = int.Parse(condition[2..]);
				return Convert.ToInt32(value) < expected;
			}
			else if (condition.StartsWith("in(", StringComparison.Ordinal))
			{
				int closePos = condition.LastIndexOf(')');
				string listStr = condition[3..closePos];
				int[] list = listStr.Split(',').Select(static x => int.Parse(x.Trim())).ToArray();
				return list.Contains(Convert.ToInt32(value));
			}
			else if (condition.StartsWith("nin(", StringComparison.Ordinal))
			{
				int closePos = condition.LastIndexOf(')');
				string listStr = condition[4..closePos];
				int[] list = listStr.Split(',').Select(static x => int.Parse(x.Trim())).ToArray();
				return !list.Contains(Convert.ToInt32(value));
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
	}
}
