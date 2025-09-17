using BMTP3.Common.MessageFormatterParser.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BMTP3.Common.MessageFormatterParser {
	public class TypeChecker {
		private readonly Dictionary<string, Type> knownValues;

		public TypeChecker(Dictionary<string, Type> knownValues) {
			this.knownValues = knownValues;
		}

		public void Validate(RootNode ast) {
			foreach(var node in ast.Children) {
				if(node is PlaceholderNode placeholder)
					ValidatePlaceholder(placeholder);
			}
		}

		private void ValidatePlaceholder(PlaceholderNode node) {
			/*
			System.Diagnostics.Debug.WriteLine($"Validating Placeholder: {node.NameOrIndex}, Type: {node.Type}"); // TODO: Debug output

			// Check if name/index exists
			if(!knownValues.ContainsKey(node.NameOrIndex))
				throw new Exception($"Undefined argument: {node.NameOrIndex}");

			var valueType = knownValues[node.NameOrIndex];

			// Validate functions
			foreach(var func in node.Functions) {
				if(!IsValidFunction(func.Name, valueType))
					throw new Exception($"Invalid function {func.Name} for type {valueType}");
			}

			// Validate type and style
			if(node.Type != null) {
				if(!IsValidType(node.Type))
					throw new Exception($"Invalid type: {node.Type}");

				// Validate that the type matches the value's type
				if(!IsTypeCompatible(node.Type, valueType))
					throw new Exception($"Invalid type {node.Type} for value type {valueType}");

				if(node.Style != null && !IsValidStyle(node.Type, node.Style))
					throw new Exception($"Invalid style {node.Style} for type {node.Type}");
			}

			// Validate pattern
			if(node.Pattern != null) {
				foreach(var item in node.Pattern) {
					if(item is PlaceholderNode nested)
						ValidatePlaceholder(nested);
				}
			}

			// Validate condition
			if(node.Condition != null) {
				if(!IsValidCondition(node.Condition.ConditionOperator))
					throw new Exception($"Invalid condition: {node.Condition.ConditionOperator}");
				foreach(var item in node.Condition.TrueValue.Concat(node.Condition.FalseValue)) {
					if(item is PlaceholderNode nested)
						ValidatePlaceholder(nested);
				}
			}
			*/
			throw new NotImplementedException();
		}

		private bool IsValidFunction(string func, Type type) {
			return (func == "toUpper" || func == "toLower" || func == "trim") && type == typeof(string) ||
				   (func == "abs" || func == "round") && type == typeof(double) ||
				   func == "toString";
		}

		private bool IsValidType(string type) => new[] { "number", "date", "string", "if" }.Contains(type);

		private bool IsTypeCompatible(string nodeType, Type valueType) {
			if(nodeType == "string") return valueType == typeof(string);
			if(nodeType == "number") return valueType == typeof(double) || valueType == typeof(int);
			if(nodeType == "date") return valueType == typeof(string); // Antager strengbaserede datoer
			if(nodeType == "if") return true; // Betingelser kan bruges med alle typer
			return false;
		}

		private bool IsValidStyle(string type, string style) {
			return (type == "number" && new[] { "integer", "currency", "percent" }.Contains(style)) ||
				   (type == "date" && new[] { "short", "long" }.Contains(style)) ||
				   (type == "string" && new[] { "upper", "lower", "title" }.Contains(style));
		}

		private bool IsValidCondition(string condition) => new[] { "eq0", "gt0", "gte0", "lt0", "lte0", "ne0", "in", "nin" }.Contains(condition);
	}
}