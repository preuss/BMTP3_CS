namespace BMTP3.MessageFormatter.Abstractions
{
	using Models;

	/// <summary>
	/// Represents a function that can be called on a value in a message template.
	/// </summary>
	public interface IFunction
	{
		/// <summary>
		/// The name of the function (e.g., "toUpper", "trim").
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Executes the function on the given value with the provided arguments.
		/// </summary>
		object? Execute(object? value, List<object> arguments);

		/// <summary>
		/// Returns the runtime type of the result.
		/// </summary>
		string GetResultType(string inputType);
	}

	/// <summary>
	/// Registers functions for a specific runtime type.
	/// </summary>
	public interface IFunctionRegistry
	{
		/// <summary>
		/// Registers a function for a type.
		/// </summary>
		void Register(string typeName, IFunction function);

		/// <summary>
		/// Gets a function by name for a type. Throws if not found.
		/// </summary>
		IFunction GetFunction(string typeName, string functionName);

		/// <summary>
		/// Checks if a function is registered for a type.
		/// </summary>
		bool TryGetFunction(string typeName, string functionName, out IFunction? function);
	}

	/// <summary>
	/// Handles formatting for a specific type (e.g., number, date, datetime, time).
	/// </summary>
	public interface IFormatType
	{
		/// <summary>
		/// Name of the format type (e.g., "number", "date").
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Asserts that the value is compatible with this FormatType.
		/// Throws FormatTypeAssertionException if not compatible.
		/// </summary>
		void AssertCompatible(object? value);

		/// <summary>
		/// Converts a value to string using default representation.
		/// </summary>
		string FormatDefault(object? value);

		/// <summary>
		/// Converts a value to string using a FormatStyle.
		/// Throws if the style is not registered.
		/// </summary>
		string FormatWithStyle(object? value, string styleName);

		/// <summary>
		/// Converts a value to string using a CustomPattern.
		/// Throws if the pattern is invalid.
		/// </summary>
		string FormatWithPattern(object? value, string pattern);

		/// <summary>
		/// Gets available FormatStyles for this type.
		/// </summary>
		IEnumerable<string> GetAvailableStyles();

		/// <summary>
		/// Checks if a FormatStyle is registered.
		/// </summary>
		bool IsStyleAvailable(string styleName);
	}

	/// <summary>
	/// Parses message templates into expressions.
	/// </summary>
	public interface IMessageParser
	{
		/// <summary>
		/// Parses a message template and returns a list of text segments and expressions.
		/// </summary>
		List<object> Parse(string template);
	}

	/// <summary>
	/// Evaluates parsed expressions with arguments.
	/// </summary>
	public interface IMessageEvaluator
	{
		/// <summary>
		/// Evaluates an expression with named arguments.
		/// </summary>
		string Evaluate(ParsedExpression expression, Dictionary<string, object?> namedArgs);

		/// <summary>
		/// Evaluates an expression with indexed arguments.
		/// </summary>
		string Evaluate(ParsedExpression expression, params object?[] indexedArgs);

		/// <summary>
		/// Evaluates an expression with both named and indexed arguments.
		/// </summary>
		string Evaluate(ParsedExpression expression, Dictionary<string, object?> namedArgs, params object?[] indexedArgs);

		/// <summary>
		/// Evaluates an expression with an evaluation context.
		/// </summary>
		string Evaluate(ParsedExpression expression, EvaluationContext context);
	}

	/// <summary>
	/// Registry for all FormatTypes.
	/// </summary>
	public interface IFormatTypeRegistry
	{
		/// <summary>
		/// Registers a FormatType.
		/// </summary>
		void Register(IFormatType formatType);

		/// <summary>
		/// Gets a FormatType by name. Case-insensitive.
		/// </summary>
		IFormatType GetFormatType(string name);

		/// <summary>
		/// Tries to get a FormatType by name. Case-insensitive.
		/// </summary>
		bool TryGetFormatType(string name, out IFormatType? formatType);
	}

	/// <summary>
	/// Main facade for message formatting.
	/// </summary>
	public interface IMessageFormatter
	{
		/// <summary>
		/// Formats a message template with named arguments.
		/// </summary>
		string Format(string template, Dictionary<string, object?> args);

		/// <summary>
		/// Formats a message template with indexed arguments.
		/// </summary>
		string Format(string template, params object?[] args);

		/// <summary>
		/// Formats a message template with both named and indexed arguments.
		/// </summary>
		string Format(string template, Dictionary<string, object?> namedArgs, params object?[] indexedArgs);

		/// <summary>
		/// Formats a message template with an evaluation context.
		/// </summary>
		string Format(string template, EvaluationContext context);
	}
}
