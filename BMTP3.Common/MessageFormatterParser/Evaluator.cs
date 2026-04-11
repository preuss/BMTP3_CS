using System.Globalization;
using System.Text;
using BMTP3.Common.MessageFormatterParser.Nodes;

namespace BMTP3.Common.MessageFormatterParser;

public class Evaluator
{
	private readonly Dictionary<string, object> values;

	public Evaluator(Dictionary<string, object> values)
	{
		this.values = values;
	}

	public string Evaluate(RootNode ast)
	{
		StringBuilder sb = new();
		foreach (AstNode node in ast.Children)
		{
			if (node is TextNode text)
			{
				sb.Append(text.Value);
			}
			else if (node is PlaceholderNode placeholder)
			{
				sb.Append(EvaluatePlaceholder(placeholder));
			}
		}

		return sb.ToString();
	}

	private string EvaluatePlaceholder(PlaceholderNode node)
	{
		if (!values.ContainsKey(node.NameOrIndex))
		{
			return string.Empty; // Be forgiving for missing values
		}

		object? value = values[node.NameOrIndex];

		if (value == null)
		{
			return string.Empty;
		}

		// Apply functions (now passing whole FunctionCallNode so args may be used)
		foreach (FunctionCallNode func in node.Functions)
		{
			value = ApplyFunction(func, value);
		}

		// Handle if condition
		if (node.Condition != null)
		{
			bool conditionMet = EvaluateCondition(node.Condition, value);
			List<AstNode>? stringToEvaluate = conditionMet ? node.Condition.TrueValue : node.Condition.FalseValue;
			RootNode tempAst = new();
			if (stringToEvaluate != null)
			{
				tempAst.Children.AddRange(stringToEvaluate);
			}

			return Evaluate(tempAst) ?? string.Empty;
		}

		// Handle pattern
		if (node.Pattern != null)
		{
			StringBuilder patternResult = new();
			foreach (AstNode item in node.Pattern)
			{
				if (item is TextNode text)
				{
					// Handle date formatting for specific patterns
					DateTime date = DateTime.MinValue;
					bool isDate = false;

					if (value is DateTime dt)
					{
						date = dt;
						isDate = true;
					}
					else if (value is string s && DateTime.TryParse(s, out DateTime parsed))
					{
						date = parsed;
						isDate = true;
					}

					if (node.NameOrIndex == "date" && isDate)
					{
						// Support common tokens; fall back to literal if unknown
						switch (text.Value)
						{
							case "yyyy": patternResult.Append(date.Year.ToString("D4")); break;
							case "MM": patternResult.Append(date.Month.ToString("D2")); break;
							case "dd": patternResult.Append(date.Day.ToString("D2")); break;
							case "HH": patternResult.Append(date.ToString("HH")); break;
							case "mm": patternResult.Append(date.ToString("mm")); break;
							case "ss": patternResult.Append(date.ToString("ss")); break;
							case "EEE":
								patternResult.Append(date.ToString("ddd", CultureInfo.InvariantCulture)
									.Substring(0, 3)); break;
							default: patternResult.Append(text.Value); break; // preserve separators
						}
					}
					else
					{
						patternResult.Append(text.Value);
					}
				}
				else if (item is LiteralNode literal)
				{
					patternResult.Append(literal.Value);
				}
				else if (item is PlaceholderNode nested)
				{
					patternResult.Append(EvaluatePlaceholder(nested));
				}
			}

			return patternResult.ToString();
		}

		// Handle type and style
		if (node.Type == "string" && node.Style == "upper")
		{
			return value.ToString()!.ToUpperInvariant();
		}

		if (node.Type == "number" && node.Style == "integer")
		{
			try
			{
				double asDouble = Convert.ToDouble(value, CultureInfo.InvariantCulture);
				return Convert.ToInt64(Math.Truncate(asDouble)).ToString(CultureInfo.InvariantCulture);
			}
			catch
			{
				return value.ToString() ?? string.Empty;
			}
		}

		// Simple placeholder: return value as string
		return value.ToString() ?? string.Empty;
	}

	private bool EvaluateCondition(IfConditionNode condition, object value)
	{
		if (condition == null)
		{
			return false;
		}

		string? op = null;
		if (condition.ConditionOperator is TextNode tn)
		{
			op = tn.Value;
		}
		else if (condition.ConditionOperator is LiteralNode ln)
		{
			op = ln.Value;
		}

		// Evaluate first parameter if present
		string? param = null;
		if (condition.ConditionParameters != null && condition.ConditionParameters.Count > 0)
		{
			AstNode p = condition.ConditionParameters[0];
			param = EvaluateArg(p);
		}

		if (string.IsNullOrEmpty(op))
		{
			return false;
		}

		// Common simple operators
		try
		{
			switch (op)
			{
				case "eq0": return Convert.ToDouble(value) == 0;
				case "gt0": return Convert.ToDouble(value) > 0;
				case "lt0": return Convert.ToDouble(value) < 0;
				case "eq":
					return string.Equals(value?.ToString(), param, StringComparison.Ordinal);
				case "ne":
					return !string.Equals(value?.ToString(), param, StringComparison.Ordinal);
				default:
					return false;
			}
		}
		catch
		{
			return false;
		}
	}

	private object ApplyFunction(FunctionCallNode funcCall, object value)
	{
		string func = funcCall.Name;

		// Helper to get first argument as string
		string? Arg0()
		{
			return funcCall.Arguments != null && funcCall.Arguments.Count > 0
				? EvaluateArg(funcCall.Arguments[0])
				: null;
		}

		if (func == "toUpper" && value is string s)
		{
			return s.ToUpperInvariant();
		}

		if (func == "toLower" && value is string s2)
		{
			return s2.ToLowerInvariant();
		}

		if (func == "abs")
		{
			try
			{
				double d = Convert.ToDouble(value, CultureInfo.InvariantCulture);
				return Math.Abs(d);
			}
			catch
			{
				return value;
			}
		}

		if (func == "format")
		{
			string fmt = Arg0() ?? string.Empty;
			try
			{
				// If the format string is a composite format like "{0:0.00}", extract inner format
				if (fmt.Contains("{") && fmt.Contains("0:") && fmt.TrimEnd().EndsWith("}"))
				{
					int colon = fmt.IndexOf(':');
					int last = fmt.LastIndexOf('}');
					if (colon > -1 && last > colon)
					{
						string inner = fmt.Substring(colon + 1, last - colon - 1);
						if (value is IFormattable f)
						{
							return f.ToString(inner, CultureInfo.InvariantCulture);
						}
					}

					return string.Format(CultureInfo.InvariantCulture, fmt, value);
				}

				if (value is IFormattable f2)
				{
					return f2.ToString(fmt, CultureInfo.InvariantCulture);
				}

				return string.Format(CultureInfo.InvariantCulture, fmt, value);
			}
			catch
			{
				return value;
			}
		}

		// Unknown function: return unchanged
		return value;
	}

	private string? EvaluateArg(AstNode node)
	{
		if (node is TextNode t)
		{
			return t.Value;
		}

		if (node is LiteralNode l)
		{
			return l.Value;
		}

		if (node is PlaceholderNode p)
		{
			return EvaluatePlaceholder(p);
		}

		return null;
	}
}