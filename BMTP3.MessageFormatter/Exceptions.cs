namespace BMTP3.MessageFormatter
{
	/// <summary>
	/// Base exception for MessageFormatter errors.
	/// </summary>
	public class MessageFormatterException : Exception
	{
		public MessageFormatterException(string message) : base(message) { }
		public MessageFormatterException(string message, Exception inner) : base(message, inner) { }
	}

	/// <summary>
	/// Thrown when a template syntax error is detected during parsing.
	/// </summary>
	public class MessageSyntaxException : MessageFormatterException
	{
		public int Position { get; }
		public string? Template { get; }

		public MessageSyntaxException(string message, int position = -1, string? template = null)
			: base(message)
		{
			Position = position;
			Template = template;
		}
	}

	/// <summary>
	/// Thrown when a runtime error occurs during evaluation.
	/// </summary>
	public class MessageEvaluationException : MessageFormatterException
	{
		public MessageEvaluationException(string message) : base(message) { }
		public MessageEvaluationException(string message, Exception inner) : base(message, inner) { }
	}

	/// <summary>
	/// Thrown when a required variable is missing during evaluation.
	/// </summary>
	public class MissingVariableException : MessageEvaluationException
	{
		public string? VariableName { get; }
		public int? VariableIndex { get; }

		public MissingVariableException(string variableName)
			: base($"Variable '{variableName}' not found")
		{
			VariableName = variableName;
		}

		public MissingVariableException(int index)
			: base($"Argument at index {index} not found")
		{
			VariableIndex = index;
		}
	}

	/// <summary>
	/// Thrown when a function is not registered for a type.
	/// </summary>
	public class FunctionNotRegisteredException : MessageEvaluationException
	{
		public FunctionNotRegisteredException(string functionName, string typeName)
			: base($"Function '{functionName}' is not registered for type '{typeName}'") { }
	}

	/// <summary>
	/// Thrown when a FormatType assertion fails.
	/// </summary>
	public class FormatTypeAssertionException : MessageEvaluationException
	{
		public FormatTypeAssertionException(string expectedType, string actualType)
			: base($"Type mismatch: expected '{expectedType}', got '{actualType}'") { }
	}

	/// <summary>
	/// Thrown when a FormatStyle is not registered for a FormatType.
	/// </summary>
	public class FormatStyleNotRegisteredException : MessageEvaluationException
	{
		public FormatStyleNotRegisteredException(string styleName, string formatTypeName)
			: base($"FormatStyle '{styleName}' is not registered for FormatType '{formatTypeName}'") { }
	}

	/// <summary>
	/// Thrown when a CustomPattern contains invalid tokens.
	/// </summary>
	public class InvalidCustomPatternException : MessageEvaluationException
	{
		public InvalidCustomPatternException(string message) : base(message) { }
	}
}
