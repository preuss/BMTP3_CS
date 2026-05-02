namespace BMTP3.MessageFormatter.Models
{
	/// <summary>
	/// Represents a lexical token from the message template.
	/// </summary>
	public class Token
	{
		public TokenType Type { get; }
		public string Value { get; }
		public int Position { get; }

		public Token(TokenType type, string value, int position)
		{
			Type = type;
			Value = value;
			Position = position;
		}

		public override string ToString() => $"{Type}:{Value}@{Position}";
	}

	/// <summary>
	/// Token types for the message formatter lexer.
	/// </summary>
	public enum TokenType
	{
		Text,                  // Plain text
		NamedPlaceholder,      // ${...}
		IndexedPlaceholder,    // #{...}
		Dot,                   // .
		Comma,                 // ,
		Colon,                 // :
		Semicolon,             // ;
		Pipe,                  // |
		Slash,                 // /
		Dash,                  // -
		Hash,                  // # (for eval patterns)
		EvalSeparator,         // § or ¶
		OpenParen,             // (
		CloseParen,            // )
		OpenBrace,             // {
		CloseBrace,            // }
		OpenBracket,           // [
		CloseBracket,          // ]
		Question,              // ?
		Identifier,            // abc, _name, func123
		Number,                // 0, 123, -45
		String,                // "..." or '...'
		Eof,
		Error
	}

	/// <summary>
	/// Represents a parsed placeholder expression.
	/// </summary>
	public class ParsedExpression
	{
		/// <summary>
		/// The variable reference (either name or index).
		/// </summary>
		public required VariableReference Variable { get; set; }

		/// <summary>
		/// Functions to apply in sequence.
		/// </summary>
		public List<FunctionCall> Functions { get; set; } = new();

		/// <summary>
		/// The type of expression (Format or Eval).
		/// </summary>
		public ExpressionType ExpressionType { get; set; }

		/// <summary>
		/// FormatType assertion (if Format expression).
		/// </summary>
		public string? FormatType { get; set; }

		/// <summary>
		/// FormatStyle name (if Format expression with style).
		/// </summary>
		public string? FormatStyle { get; set; }

		/// <summary>
		/// CustomPattern (if Format expression with pattern).
		/// </summary>
		public string? CustomPattern { get; set; }

		/// <summary>
		/// EvalType (if, plural, select - for Eval expressions).
		/// </summary>
		public string? EvalType { get; set; }

		/// <summary>
		/// EvalPattern (for Eval expressions).
		/// </summary>
		public string? EvalPattern { get; set; }
	}

	/// <summary>
	/// Variable reference (either named or indexed).
	/// </summary>
	public abstract class VariableReference
	{
		public int Position { get; set; }
	}

	/// <summary>
	/// Named variable reference (e.g., ${name}).
	/// </summary>
	public class NamedVariableReference : VariableReference
	{
		public string Name { get; set; }

		public NamedVariableReference(string name)
		{
			Name = name;
		}

		public override string ToString() => $"${{{Name}}}";
	}

	/// <summary>
	/// Indexed variable reference (e.g., #{0}).
	/// </summary>
	public class IndexedVariableReference : VariableReference
	{
		public int Index { get; set; }

		public IndexedVariableReference(int index)
		{
			Index = index;
		}

		public override string ToString() => $"#{{{Index}}}";
	}

	/// <summary>
	/// Represents a function call.
	/// </summary>
	public class FunctionCall
	{
		public string Name { get; set; }
		public List<object> Arguments { get; set; } = new();

		public FunctionCall(string name)
		{
			Name = name;
		}

		public override string ToString() => $"{Name}({string.Join(", ", Arguments)})";
	}

	/// <summary>
	/// Type of expression.
	/// </summary>
	public enum ExpressionType
	{
		Format,  // Format expression: ${name, FormatType, FormatStyle} or ${name, FormatType : CustomPattern}
		Eval     // Eval expression: ${name § EvalType, EvalPattern}
	}

	/// <summary>
	/// Runtime evaluation context.
	/// </summary>
	public class EvaluationContext
	{
		private readonly Dictionary<string, object?> _namedArgs = new(StringComparer.Ordinal);
		private readonly List<object?> _indexedArgs = new();

		public EvaluationContext(params object?[] args)
		{
			_indexedArgs.AddRange(args);
		}

		public EvaluationContext(Dictionary<string, object?> namedArgs, params object?[] indexedArgs)
		{
			_namedArgs = new Dictionary<string, object?>(namedArgs, StringComparer.Ordinal);
			_indexedArgs.AddRange(indexedArgs);
		}

		public object? GetNamedArgument(string name)
		{
			if (_namedArgs.TryGetValue(name, out object? value))
				return value;
			throw new MissingVariableException(name);
		}

		public object? GetIndexedArgument(int index)
		{
			if (index < 0 || index >= _indexedArgs.Count)
				throw new MissingVariableException(index);
			return _indexedArgs[index];
		}

		public void AddNamedArgument(string name, object? value)
		{
			_namedArgs[name] = value;
		}

		public void AddIndexedArgument(object? value)
		{
			_indexedArgs.Add(value);
		}

		public bool HasNamedArgument(string name) => _namedArgs.ContainsKey(name);
		public bool HasIndexedArgument(int index) => index >= 0 && index < _indexedArgs.Count;
	}
}
