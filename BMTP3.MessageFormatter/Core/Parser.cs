namespace BMTP3.MessageFormatter.Core
{
	using Models;
	using Abstractions;
	using System.Text;

	/// <summary>
	/// Parses message templates into expressions and text segments.
	/// </summary>
	public class MessageParser : IMessageParser
	{
		public List<object> Parse(string template)
		{
			Lexer lexer = new(template);
			List<Token> tokens = lexer.Tokenize();

			List<object> result = new();

			foreach (Token token in tokens)
			{
				if (token.Type == TokenType.Text)
				{
					result.Add(token.Value);
				}
				else if (token.Type == TokenType.NamedPlaceholder)
				{
					result.Add(ParsePlaceholder(token.Value, isIndexed: false));
				}
				else if (token.Type == TokenType.IndexedPlaceholder)
				{
					result.Add(ParsePlaceholder(token.Value, isIndexed: true));
				}
				else if (token.Type == TokenType.Eof)
				{
					break;
				}
			}

			return result;
		}

		private ParsedExpression ParsePlaceholder(string content, bool isIndexed)
		{
			PlaceholderTokenizer tokenizer = new(content);
			List<Token> tokens = tokenizer.Tokenize();

			PlaceholderParser parser = new(tokens, content);
			return parser.Parse(isIndexed);
		}
	}

	/// <summary>
	/// Parses the content of a placeholder expression.
	/// </summary>
	internal class PlaceholderParser
	{
		private readonly List<Token> _tokens;
		private readonly string _originalContent;
		private int _pos;

		public PlaceholderParser(List<Token> tokens, string originalContent)
		{
			_tokens = tokens;
			_originalContent = originalContent;
			_pos = 0;
		}

		public ParsedExpression Parse(bool isIndexed)
		{
			ParsedExpression expr = new() { Variable = null! }; // Will be set below

			// Parse variable reference
			expr.Variable = ParseVariableReference(isIndexed);

			// Parse functions
			while (Current().Type == TokenType.Dot)
			{
				Advance(); // consume dot
				expr.Functions.Add(ParseFunctionCall());
			}

			// Check if eval or format expression
			if (Current().Type == TokenType.EvalSeparator)
			{
				ParseEvalExpression(expr);
			}
			else if (Current().Type == TokenType.Comma || Current().Type == TokenType.Colon)
			{
				ParseFormatExpression(expr);
			}

			ExpectEnd();
			return expr;
		}

		private VariableReference ParseVariableReference(bool isIndexed)
		{
			if (isIndexed)
			{
				Token numToken = Expect(TokenType.Number);
				int index = int.Parse(numToken.Value);
				return new IndexedVariableReference(index);
			}
			else
			{
				Token idToken = Expect(TokenType.Identifier);
				return new NamedVariableReference(idToken.Value);
			}
		}

		private FunctionCall ParseFunctionCall()
		{
			Token nameToken = Expect(TokenType.Identifier);
			Expect(TokenType.OpenParen);

			FunctionCall call = new(nameToken.Value);

			if (Current().Type != TokenType.CloseParen)
			{
				while (true)
				{
					call.Arguments.Add(ParseFunctionArgument());

					if (Current().Type == TokenType.CloseParen)
						break;

					Expect(TokenType.Comma);
				}
			}

			Expect(TokenType.CloseParen);
			return call;
		}

		private object ParseFunctionArgument()
		{
			if (Current().Type == TokenType.String)
			{
				Token strToken = Advance();
				return strToken.Value;
			}
			else if (Current().Type == TokenType.Number)
			{
				Token numToken = Advance();
				if (int.TryParse(numToken.Value, out int intVal))
					return intVal;
				return double.Parse(numToken.Value);
			}
			else if (Current().Type == TokenType.Identifier)
			{
				Token idToken = Advance();
				return idToken.Value;
			}
			else
			{
				throw new MessageSyntaxException($"Invalid function argument: {Current().Value}");
			}
		}

		private void ParseFormatExpression(ParsedExpression expr)
		{
			string? formatType = null;
			string? formatStyle = null;
			string? customPattern = null;

			if (Current().Type == TokenType.Comma)
			{
				Advance(); // consume comma

				// FormatType is required after comma in format-expression context
				formatType = Expect(TokenType.Identifier).Value;

				// FormatStyle
				if (Current().Type == TokenType.Comma)
				{
					Advance(); // consume comma
					formatStyle = Expect(TokenType.Identifier).Value;
				}

				// CustomPattern
				if (Current().Type == TokenType.Colon)
				{
					if (!string.IsNullOrWhiteSpace(formatStyle))
					{
						throw new MessageSyntaxException("FormatStyle and CustomPattern cannot be used together");
					}

					Advance(); // consume colon
					customPattern = ReadUntilEnd();
				}
			}
			else if (Current().Type == TokenType.Colon)
			{
				throw new MessageSyntaxException("FormatType is required when using CustomPattern");
			}

			if (string.IsNullOrWhiteSpace(formatType) && !string.IsNullOrWhiteSpace(formatStyle))
			{
				throw new MessageSyntaxException("FormatType is required when using FormatStyle");
			}

			expr.FormatType = formatType;
			expr.FormatStyle = formatStyle;
			expr.CustomPattern = customPattern;
			expr.ExpressionType = ExpressionType.Format;
		}

		private void ParseEvalExpression(ParsedExpression expr)
		{
			Advance(); // consume eval separator

			// EvalType
			expr.EvalType = Expect(TokenType.Identifier).Value;

			Expect(TokenType.Comma);

			// EvalPattern
			expr.EvalPattern = ReadUntilEnd();

			expr.ExpressionType = ExpressionType.Eval;

			if (!string.IsNullOrWhiteSpace(expr.FormatType) || !string.IsNullOrWhiteSpace(expr.FormatStyle) || !string.IsNullOrWhiteSpace(expr.CustomPattern))
			{
				throw new MessageSyntaxException("Eval expression and Format expression cannot be used together");
			}
		}

		private string ReadUntilEnd()
		{
			// Read the remaining part of the expression from the original content
			// to preserve whitespace and formatting
			if (_pos >= _tokens.Count || Current().Type == TokenType.Eof)
				return string.Empty;

			// Find the position in original content where we are
			// by checking the last token's position + value
			int startPos = 0;
			for (int i = 0; i < _pos && i < _tokens.Count; i++)
			{
				Token tok = _tokens[i];
				// Find this token value in original starting from startPos
				int idx = _originalContent.IndexOf(tok.Value, startPos, StringComparison.Ordinal);
				if (idx >= 0)
					startPos = idx + tok.Value.Length;
			}

			// Now read from startPos to the end
			string result = _originalContent.Substring(startPos).Trim();
			
			// Skip past all remaining tokens
			while (_pos < _tokens.Count && Current().Type != TokenType.Eof)
				_pos++;

			return result;
		}

		private Token Current() => _pos < _tokens.Count ? _tokens[_pos] : new Token(TokenType.Eof, string.Empty, -1);
		private Token Advance() => _tokens[_pos++];

		private Token Expect(TokenType type)
		{
			if (Current().Type != type)
				throw new MessageSyntaxException($"Expected {type}, got {Current().Type}: {Current().Value}");
			return Advance();
		}

		private void ExpectEnd()
		{
			if (Current().Type != TokenType.Eof)
				throw new MessageSyntaxException($"Unexpected token: {Current().Value}");
		}
	}
}
