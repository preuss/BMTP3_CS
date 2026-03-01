using BMTP3.Common.MessageFormatterParser.Nodes;

namespace BMTP3.Common.MessageFormatterParser
{
	public class TypeChecker
	{
		private readonly Dictionary<string, Type> knownValues;

		public TypeChecker(Dictionary<string, Type> knownValues)
		{
			this.knownValues = knownValues;
		}

		public void Validate(RootNode ast)
		{
			foreach(var node in ast.Children)
			{
				if(node is PlaceholderNode placeholder)
					ValidatePlaceholder(placeholder);
			}
		}

		private void ValidatePlaceholder(PlaceholderNode node)
		{
			// Minimal implementation: check the placeholder name exists and validate nested placeholders and condition branches
			if(!knownValues.ContainsKey(node.NameOrIndex))
				throw new KeyNotFoundException($"Undefined argument: {node.NameOrIndex}");

			// Validate functions against known value type
			var valueType = knownValues[node.NameOrIndex];
			foreach(var func in node.Functions)
			{
				if(!IsValidFunction(func.Name, valueType))
					throw new InvalidOperationException($"Invalid function {func.Name} for type {valueType}");
			}

			// Validate pattern nested placeholders
			if(node.Pattern != null)
			{
				foreach(var item in node.Pattern)
				{
					if(item is PlaceholderNode nested)
						ValidatePlaceholder(nested);
				}
			}

			// Validate condition branches
			if(node.Condition != null)
			{
				// basic check of operator name (ConditionOperator is an AstNode - we treat TextNode)
				var opText = (node.Condition.ConditionOperator as TextNode)?.Value ?? string.Empty;
				if(!IsValidCondition(opText))
					throw new InvalidOperationException($"Invalid condition: {opText}");

				foreach(var item in node.Condition.TrueValue.Concat(node.Condition.FalseValue))
				{
					if(item is PlaceholderNode nested)
						ValidatePlaceholder(nested);
				}
			}
		}

		private bool IsValidFunction(string func, Type type)
		{
			return (func == "toUpper" || func == "toLower" || func == "trim") && type == typeof(string) ||
				   (func == "abs" || func == "round") && type == typeof(double) ||
				   func == "toString";
		}

		private bool IsValidType(string type) => new[] { "number", "date", "string", "if" }.Contains(type);

		private bool IsTypeCompatible(string nodeType, Type valueType)
		{
			if(nodeType == "string") return valueType == typeof(string);
			if(nodeType == "number") return valueType == typeof(double) || valueType == typeof(int);
			if(nodeType == "date") return valueType == typeof(string); // Antager strengbaserede datoer
			if(nodeType == "if") return true; // Betingelser kan bruges med alle typer
			return false;
		}

		private bool IsValidStyle(string type, string style)
		{
			return (type == "number" && new[] { "integer", "currency", "percent" }.Contains(style)) ||
				   (type == "date" && new[] { "short", "long" }.Contains(style)) ||
				   (type == "string" && new[] { "upper", "lower", "title" }.Contains(style));
		}

		private bool IsValidCondition(string condition) => new[] { "eq0", "gt0", "gte0", "lt0", "lte0", "ne0", "in", "nin" }.Contains(condition);
	}
}
