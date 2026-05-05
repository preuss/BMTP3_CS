namespace BMTP3.MessageFormatter.Core
{
	using Models;
	using System.Text;

	/// <summary>
	/// Lexical analyzer for message templates.
	/// Converts a template string into a sequence of tokens.
	/// </summary>
	public class Lexer
	{
		private readonly string _input;
		private int _pos;

		public Lexer(string input)
		{
			_input = input ?? throw new ArgumentNullException(nameof(input));
			_pos = 0;
		}

		/// <summary>
		/// Tokenizes the input template.
		/// </summary>
		public List<Token> Tokenize()
		{
			List<Token> tokens = new();
			int loopCount = 0;
			const int maxLoops = 10000;

			while (!IsAtEnd() && loopCount < maxLoops)
			{
				loopCount++;
				if (Current() == '$' && Peek() == '{')
				{
					tokens.Add(ReadNamedPlaceholder());
				}
				else if (Current() == '#' && Peek() == '{')
				{
					tokens.Add(ReadIndexedPlaceholder());
				}
				else
				{
					tokens.Add(ReadText());
				}
			}

			if (loopCount >= maxLoops)
				throw new Exception($"Lexer infinite loop detected: loopCount={loopCount}, pos={_pos}, length={_input.Length}");

			tokens.Add(new Token(TokenType.Eof, string.Empty, _pos));
			return tokens;
		}

		private Token ReadText()
		{
			int start = _pos;
			StringBuilder sb = new();

			while (!IsAtEnd() && Current() != '$' && Current() != '#')
			{
				if (Current() == '\\')
				{
					// Handle escape sequences in plain text
					_pos++;
					if (!IsAtEnd())
					{
						sb.Append(Current());
						_pos++;
					}
				}
				else
				{
					sb.Append(Current());
					_pos++;
				}
			}

			return new Token(TokenType.Text, sb.ToString(), start);
		}

		private Token ReadNamedPlaceholder()
		{
			int start = _pos;
			Expect('$');
			Expect('{');

			int depth = 1;
			StringBuilder content = new();

			while (depth > 0 && !IsAtEnd())
			{
				if (Current() == '\\' && (Peek() == '{' || Peek() == '}'))
				{
					content.Append(Current());
					_pos++;
					content.Append(Current());
					_pos++;
				}
				else if (Current() == '{' && Peek() == '{')
				{
					content.Append(Current());
					_pos++;
					content.Append(Current());
					_pos++;
				}
				else if (Current() == '}' && Peek() == '}' && depth == 1)
				{
					if (!HasClosingBraceAhead(_pos + 2))
					{
						content.Append('}');
						_pos += 2;
						depth--;
					}
					else
					{
						content.Append(Current());
						_pos++;
						content.Append(Current());
						_pos++;
					}
				}
				else if (Current() == '{')
				{
					depth++;
					content.Append(Current());
					_pos++;
				}
				else if (Current() == '}')
				{
					depth--;
					if (depth > 0)
						content.Append(Current());
					_pos++;
				}
				else
				{
					content.Append(Current());
					_pos++;
				}
			}

			if (depth != 0)
				throw new MessageSyntaxException("Unbalanced '}' in expression", start, _input);

			return new Token(TokenType.NamedPlaceholder, content.ToString(), start);
		}

		private Token ReadIndexedPlaceholder()
		{
			int start = _pos;
			Expect('#');
			Expect('{');

			int depth = 1;
			StringBuilder content = new();

			while (depth > 0 && !IsAtEnd())
			{
				if (Current() == '\\' && (Peek() == '{' || Peek() == '}'))
				{
					content.Append(Current());
					_pos++;
					content.Append(Current());
					_pos++;
				}
				else if (Current() == '{' && Peek() == '{')
				{
					content.Append(Current());
					_pos++;
					content.Append(Current());
					_pos++;
				}
				else if (Current() == '}' && Peek() == '}' && depth == 1)
				{
					if (!HasClosingBraceAhead(_pos + 2))
					{
						content.Append('}');
						_pos += 2;
						depth--;
					}
					else
					{
						content.Append(Current());
						_pos++;
						content.Append(Current());
						_pos++;
					}
				}
				else if (Current() == '{')
				{
					depth++;
					content.Append(Current());
					_pos++;
				}
				else if (Current() == '}')
				{
					depth--;
					if (depth > 0)
						content.Append(Current());
					_pos++;
				}
				else
				{
					content.Append(Current());
					_pos++;
				}
			}

			if (depth != 0)
				throw new MessageSyntaxException("Unbalanced '}' in expression", start, _input);

			return new Token(TokenType.IndexedPlaceholder, content.ToString(), start);
		}

		private bool HasClosingBraceAhead(int fromIndex)
		{
			for (int i = fromIndex; i < _input.Length; i++)
			{
				if (_input[i] == '}')
				{
					return true;
				}
			}

			return false;
		}

		private char Current() => _pos < _input.Length ? _input[_pos] : '\0';
		private char Peek() => _pos + 1 < _input.Length ? _input[_pos + 1] : '\0';

		private bool IsAtEnd() => _pos >= _input.Length;

		private void Expect(char c)
		{
			if (Current() != c)
				throw new MessageSyntaxException($"Expected '{c}' but got '{Current()}'", _pos, _input);
			_pos++;
		}
	}

	/// <summary>
	/// Tokenizes the content inside a placeholder (${...} or #{...}).
	/// </summary>
	public class PlaceholderTokenizer
	{
		private readonly string _input;
		private int _pos;

		public PlaceholderTokenizer(string input)
		{
			_input = input ?? throw new ArgumentNullException(nameof(input));
			_pos = 0;
		}

		/// <summary>
		/// Tokenizes placeholder content.
		/// </summary>
		public List<Token> Tokenize()
		{
			List<Token> tokens = new();
			int loopCount = 0;
			const int maxLoops = 10000;
			bool inFunctionArguments = false;
			bool expectingFunctionArgument = false;

			SkipWhitespace();

			while (!IsAtEnd() && loopCount < maxLoops)
			{
				loopCount++;
				SkipWhitespace();

				if (IsAtEnd())
				{
					break;
				}

				if (inFunctionArguments && expectingFunctionArgument)
				{
					if (Current() == ')')
					{
						tokens.Add(new Token(TokenType.CloseParen, ")", _pos++));
						inFunctionArguments = false;
						expectingFunctionArgument = false;
						continue;
					}

					tokens.Add(ReadFunctionArgumentToken());
					expectingFunctionArgument = false;
					continue;
				}

				char ch = Current();

				if (char.IsLetter(ch) || ch == '_')
				{
					tokens.Add(ReadIdentifier());
				}
				else if (ch == '.')
				{
					tokens.Add(new Token(TokenType.Dot, ".", _pos++));
				}
				else if (ch == ',')
				{
					tokens.Add(new Token(TokenType.Comma, ",", _pos++));
					if (inFunctionArguments)
					{
						expectingFunctionArgument = true;
					}
				}
				else if (ch == ':')
				{
					tokens.Add(new Token(TokenType.Colon, ":", _pos++));
				}
				else if (ch == '|')
				{
					tokens.Add(new Token(TokenType.Pipe, "|", _pos++));
				}
				else if (ch == '/')
				{
					tokens.Add(new Token(TokenType.Slash, "/", _pos++));
				}
				else if (ch == '#')
				{
					tokens.Add(new Token(TokenType.Hash, "#", _pos++));
				}
				else if (ch == '(')
				{
					tokens.Add(new Token(TokenType.OpenParen, "(", _pos++));
					inFunctionArguments = true;
					expectingFunctionArgument = true;
				}
				else if (ch == ')')
				{
					tokens.Add(new Token(TokenType.CloseParen, ")", _pos++));
					inFunctionArguments = false;
					expectingFunctionArgument = false;
				}
				else if (ch == '{')
				{
					tokens.Add(new Token(TokenType.OpenBrace, "{", _pos++));
				}
				else if (ch == '}')
				{
					tokens.Add(new Token(TokenType.CloseBrace, "}", _pos++));
				}
				else if (ch == '[')
				{
					tokens.Add(new Token(TokenType.OpenBracket, "[", _pos++));
				}
				else if (ch == ']')
				{
					tokens.Add(new Token(TokenType.CloseBracket, "]", _pos++));
				}
				else if (ch == '?')
				{
					tokens.Add(new Token(TokenType.Question, "?", _pos++));
				}
				else if (ch == '"' || ch == '\'')
				{
					tokens.Add(ReadString());
				}
				else if (ch == '-')
				{
					if (_pos + 1 < _input.Length && char.IsDigit(_input[_pos + 1]))
					{
						tokens.Add(ReadNumber());
					}
					else
					{
						tokens.Add(new Token(TokenType.Dash, "-", _pos++));
					}
				}
				else if (char.IsDigit(ch))
				{
					tokens.Add(ReadNumber());
				}
				else if (ch == '§' || ch == '¶')
				{
					tokens.Add(new Token(TokenType.EvalSeparator, ch.ToString(), _pos++));
				}
				else if (ch == ';')
				{
					tokens.Add(new Token(TokenType.Semicolon, ";", _pos++));
				}
				else if (ch == '$')
				{
					tokens.Add(new Token(TokenType.Text, ch.ToString(), _pos++));
				}
				else
				{
					throw new MessageSyntaxException($"Unexpected character '{ch}'", _pos, _input);
				}
			}

			if (loopCount >= maxLoops)
			{
				throw new Exception($"PlaceholderTokenizer infinite loop detected: loopCount={loopCount}, pos={_pos}, length={_input.Length}");
			}

			if (inFunctionArguments)
			{
				throw new MessageSyntaxException("Unbalanced ')' in function arguments", _pos, _input);
			}

			tokens.Add(new Token(TokenType.Eof, string.Empty, _pos));
			return tokens;
		}

		private Token ReadFunctionArgumentToken()
		{
			if (Current() == '"' || Current() == '\'')
			{
				return ReadFunctionQuotedArgument();
			}

			return ReadFunctionUnquotedArgument();
		}

		private Token ReadFunctionUnquotedArgument()
		{
			int start = _pos;
			StringBuilder sb = new();

			while (!IsAtEnd())
			{
				char ch = Current();
				if (ch == ',' || ch == ')')
				{
					break;
				}

				if (ch == '\\')
				{
					if (_pos + 1 >= _input.Length)
					{
						throw new MessageSyntaxException("Invalid escape sequence '\\'", _pos, _input);
					}

					char escaped = _input[_pos + 1];
					if (escaped == '\\' || escaped == ',' || escaped == ')' || escaped == '"' || escaped == '\'' || escaped == '(' || escaped == '{' || escaped == '}')
					{
						sb.Append(escaped);
						_pos += 2;
						continue;
					}

					throw new MessageSyntaxException($"Invalid escape sequence '\\{escaped}'", _pos, _input);
				}

				if (ch == '(' || ch == '{' || ch == '}')
				{
					throw new MessageSyntaxException($"Invalid character '{ch}' in unquoted argument. Escape it with '\\{ch}'", _pos, _input);
				}

				sb.Append(ch);
				_pos++;
			}

			string value = sb.ToString().Trim();
			return new Token(TokenType.String, value, start);
		}

		private Token ReadFunctionQuotedArgument()
		{
			int start = _pos;
			char quote = Current();
			_pos++;
			StringBuilder sb = new();

			while (!IsAtEnd() && Current() != quote)
			{
				if (Current() == '\\')
				{
					if (_pos + 1 >= _input.Length)
					{
						throw new MessageSyntaxException("Unterminated string", start, _input);
					}

					char escaped = _input[_pos + 1];
					if (escaped == '\\')
					{
						sb.Append('\\');
						_pos += 2;
						continue;
					}

					if (quote == '"' && escaped == '"')
					{
						sb.Append('"');
						_pos += 2;
						continue;
					}

					if (quote == '\'' && escaped == '\'')
					{
						sb.Append('\'');
						_pos += 2;
						continue;
					}

					throw new MessageSyntaxException($"Invalid escape sequence '\\{escaped}'", _pos, _input);
				}

				sb.Append(Current());
				_pos++;
			}

			if (IsAtEnd())
			{
				throw new MessageSyntaxException("Unterminated string", start, _input);
			}

			_pos++; // consume closing quote

			int checkPos = _pos;
			while (checkPos < _input.Length && char.IsWhiteSpace(_input[checkPos]))
			{
				checkPos++;
			}

			if (checkPos < _input.Length && _input[checkPos] != ',' && _input[checkPos] != ')')
			{
				throw new MessageSyntaxException("quotes must enclose the entire argument", checkPos, _input);
			}

			return new Token(TokenType.String, sb.ToString(), start);
		}

		private Token ReadIdentifier()
		{
			int start = _pos;
			StringBuilder sb = new();

			while (!IsAtEnd() && (char.IsLetterOrDigit(Current()) || Current() == '_'))
			{
				sb.Append(Current());
				_pos++;
			}

			return new Token(TokenType.Identifier, sb.ToString(), start);
		}

		private Token ReadNumber()
		{
			int start = _pos;
			StringBuilder sb = new();

			if (Current() == '-')
			{
				sb.Append(Current());
				_pos++;
			}

			while (!IsAtEnd() && char.IsDigit(Current()))
			{
				sb.Append(Current());
				_pos++;
			}

			return new Token(TokenType.Number, sb.ToString(), start);
		}

		private Token ReadString()
		{
			int start = _pos;
			char quote = Current();
			_pos++;
			StringBuilder sb = new();

			while (!IsAtEnd() && Current() != quote)
			{
				if (Current() == '\\' && Peek() == quote)
				{
					_pos++;
					sb.Append(quote);
					_pos++;
				}
				else if (Current() == '\\' && Peek() == '\\')
				{
					_pos++;
					sb.Append('\\');
					_pos++;
				}
				else
				{
					sb.Append(Current());
					_pos++;
				}
			}

			if (IsAtEnd())
			{
				throw new MessageSyntaxException("Unterminated string", start, _input);
			}

			_pos++; // consume closing quote

			return new Token(TokenType.String, sb.ToString(), start);
		}

		private char Current() => _pos < _input.Length ? _input[_pos] : '\0';
		private char Peek() => _pos + 1 < _input.Length ? _input[_pos + 1] : '\0';
		private bool IsAtEnd() => _pos >= _input.Length;
		private void SkipWhitespace()
		{
			while (!IsAtEnd() && char.IsWhiteSpace(Current()))
			{
				_pos++;
			}
		}
	}
}
