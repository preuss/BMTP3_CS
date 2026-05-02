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
			// Step 1: Resolve variable
			object? value = ResolveVariable(expression.Variable, context);
			string currentType = GetTypeName(value);

			// Step 2: Execute functions
			foreach (FunctionCall funcCall in expression.Functions)
			{
				value = ExecuteFunction(funcCall, value, currentType);
				currentType = GetTypeName(value);
			}

			// Step 3: Handle expression type
			if (expression.ExpressionType == ExpressionType.Format)
			{
				return EvaluateFormatExpression(value, currentType, expression);
			}
			else if (expression.ExpressionType == ExpressionType.Eval)
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
			else if (variable is IndexedVariableReference indexed)
			{
				return context.GetIndexedArgument(indexed.Index);
			}

			throw new MessageEvaluationException("Unknown variable reference type");
		}

		private object? ExecuteFunction(FunctionCall call, object? value, string currentType)
		{
			if (!_functionRegistry.TryGetFunction(currentType, call.Name, out IFunction? function))
				throw new FunctionNotRegisteredException(call.Name, currentType);

			return function!.Execute(value, call.Arguments);
		}

		private string EvaluateFormatExpression(object? value, string currentType, ParsedExpression expr)
		{
			// FormatType assertion (if specified)
			if (!string.IsNullOrEmpty(expr.FormatType))
			{
				IFormatType formatType = _formatTypeRegistry.GetFormatType(expr.FormatType);
				formatType.AssertCompatible(value);

				// Apply style or pattern
				if (!string.IsNullOrEmpty(expr.FormatStyle))
				{
					return formatType.FormatWithStyle(value, expr.FormatStyle);
				}
				else if (!string.IsNullOrEmpty(expr.CustomPattern))
				{
					return formatType.FormatWithPattern(value, expr.CustomPattern);
				}
				else
				{
					return formatType.FormatDefault(value);
				}
			}

			// No FormatType - use default string representation
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
			// Pattern: condition ? trueValue : falseValue
			int questionPos = pattern.IndexOf('?');
			int colonPos = pattern.LastIndexOf(':');

			if (questionPos < 0 || colonPos < 0 || questionPos > colonPos)
				throw new MessageEvaluationException("Invalid if pattern syntax");

			string condition = pattern[..questionPos].Trim();
			string trueValue = pattern[(questionPos + 1)..colonPos].Trim();
			string falseValue = pattern[(colonPos + 1)..].Trim();

			bool conditionMet = EvaluateCondition(value, condition);

			// Only evaluate nested placeholders if the string contains ${ or #{
			string selectedValue = conditionMet ? trueValue : falseValue;
			if (selectedValue.Contains("${") || selectedValue.Contains("#{"))
				return EvaluateNestedPlaceholders(selectedValue, context);
			return selectedValue;
		}

		private string EvaluatePluralExpression(object? value, string pattern, EvaluationContext context)
		{
			if (!(value is int || value is long || value is double || value is decimal))
				throw new MessageEvaluationException("Plural expressions require numeric values");

			double numValue = Convert.ToDouble(value);

			// Split by | to get rules
			List<string> rules = pattern.Split('|').Select(r => r.Trim()).ToList();

			foreach (string rule in rules)
			{
				int hashPos = rule.IndexOf('#');
				if (hashPos < 0)
					continue;

				string ruleType = rule[..hashPos].Trim();
				string ruleValue = rule[(hashPos + 1)..].Trim();

				if (MatchesPluralRule(numValue, ruleType))
				{
					// Only evaluate nested if it contains placeholders
					if (ruleValue.Contains("${") || ruleValue.Contains("#{"))
						return EvaluateNestedPlaceholders(ruleValue, context);
					return ruleValue;
				}
			}

			// No match - check for 'other' rule
			throw new MessageEvaluationException($"No matching plural rule for value {numValue} and no 'other' fallback");
		}

		private string EvaluateSelectExpression(object? value, string pattern, EvaluationContext context)
		{
			string stringValue = value?.ToString() ?? string.Empty;

			// Split by | to get rules
			List<string> rules = pattern.Split('|').Select(r => r.Trim()).ToList();

			foreach (string rule in rules)
			{
				int hashPos = rule.IndexOf('#');
				if (hashPos < 0)
					continue;

				string category = rule[..hashPos].Trim();
				string categoryValue = rule[(hashPos + 1)..].Trim();

				if (category == stringValue || category == "other")
				{
					// Only evaluate nested if it contains placeholders
					if (categoryValue.Contains("${") || categoryValue.Contains("#{"))
						return EvaluateNestedPlaceholders(categoryValue, context);
					return categoryValue;
				}
			}

			throw new MessageEvaluationException($"No matching select rule for value '{stringValue}' and no 'other' fallback");
		}

		private bool EvaluateCondition(object? value, string condition)
		{
			if (condition.StartsWith("eq"))
			{
				int expected = int.Parse(condition[2..]);
				return Convert.ToInt32(value) == expected;
			}
			else if (condition.StartsWith("ne"))
			{
				int expected = int.Parse(condition[2..]);
				return Convert.ToInt32(value) != expected;
			}
			else if (condition.StartsWith("gte"))
			{
				int expected = int.Parse(condition[3..]);
				return Convert.ToInt32(value) >= expected;
			}
			else if (condition.StartsWith("gt"))
			{
				int expected = int.Parse(condition[2..]);
				return Convert.ToInt32(value) > expected;
			}
			else if (condition.StartsWith("lte"))
			{
				int expected = int.Parse(condition[3..]);
				return Convert.ToInt32(value) <= expected;
			}
			else if (condition.StartsWith("lt"))
			{
				int expected = int.Parse(condition[2..]);
				return Convert.ToInt32(value) < expected;
			}
			else if (condition.StartsWith("in("))
			{
				int closePos = condition.LastIndexOf(')');
				string listStr = condition[3..closePos];
				int[] list = listStr.Split(',').Select(x => int.Parse(x.Trim())).ToArray();
				return list.Contains(Convert.ToInt32(value));
			}
			else if (condition.StartsWith("nin("))
			{
				int closePos = condition.LastIndexOf(')');
				string listStr = condition[4..closePos];
				int[] list = listStr.Split(',').Select(x => int.Parse(x.Trim())).ToArray();
				return !list.Contains(Convert.ToInt32(value));
			}

			throw new MessageEvaluationException($"Unknown condition: {condition}");
		}

	private bool MatchesPluralRule(double value, string rule)
	{
		if (int.TryParse(rule, out int exactValue))
			return (int)value == exactValue;

		if (rule == "other")
			return true;

		// Handle CLDR plural categories (one, few, many, zero)
		if (rule == "zero")
			return value == 0;

		if (rule == "one")
			return value == 1;

		if (rule == "few")
			return value == 2 || value == 3 || value == 4;

		if (rule == "many")
			return value >= 5;

		// Handle ranges: [0;10], ]0;10[, etc.
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
			// Protect against infinite recursion by using a depth counter
			const int maxDepth = 10;
			return EvaluateNestedPlaceholdersInternal(text, context, 0, maxDepth);
		}

		private string EvaluateNestedPlaceholdersInternal(string text, EvaluationContext context, int depth, int maxDepth)
		{
			if (depth > maxDepth)
				throw new MessageEvaluationException("Maximum nesting depth exceeded");

			// Handle escape sequences: }} -> }, {{ -> {
			text = text.Replace("}}", "§RBRACE§").Replace("{{", "§LBRACE§");

			// Simple recursive placeholder evaluation
			MessageParser parser = new();
			List<object> parsed = parser.Parse(text);

			StringBuilder sb = new();
			foreach (object item in parsed)
			{
				if (item is string str)
				{
					// Restore escaped braces
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
				return "null";
			if (value is string)
				return "string";
			if (value is int or long)
				return "integer";
			if (value is double or float or decimal)
				return "number";
			if (value is DateTime dt)
				return dt.TimeOfDay == TimeSpan.Zero ? "date" : "datetime";
			if (value is DateOnly)
				return "date";
			if (value is TimeOnly)
				return "time";
			return value.GetType().Name.ToLowerInvariant();
		}
	}
}
