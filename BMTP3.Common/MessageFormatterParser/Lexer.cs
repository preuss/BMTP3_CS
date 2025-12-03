using System.Text;

namespace BMTP3.Common.MessageFormatterParser
{
	public class Lexer : ILexer
	{
		private static readonly HashSet<string> knownFunctions = new HashSet<string>
		{
			"toUpper", "toLower", "trim", "abs", "round", "toString"
		};
		private static readonly HashSet<string> knownTypes = new HashSet<string>
		{
			"number", "string", "date", "if"
		};
		private static readonly HashSet<string> knownEvalTypes = new HashSet<string>
		{
			"eq0", "gt0", "gte0", "lt0", "lte0", "ne0", "in", "nin"
		};
		private static readonly HashSet<string> knownStyles = new HashSet<string>
		{
			"decimal", "currency", "percent", "date", "time", "datetime"
		};
		private readonly CharStream _charStream;
		private LexerState _state;
		private int _position;

		public Lexer(string input)
		{
			_charStream = new CharStream(input);
			_state = LexerState.Base;
			_position = 0;
		}

		public Token NextToken()
		{
			if(_state == LexerState.EOF)
			{
				throw new InvalidOperationException("Cannot scan token after EOF");
			}

			// Check for EOF but be careful if we are in a state that expects more input.
			if(IsEOF())
			{
				if(_state == LexerState.Placeholder)
				{
					throw new InvalidOperationException("Unterminated placeholder");
				}
				_state = LexerState.EOF;
				return new Token(TokenType.EOF, string.Empty, _position, _charStream.LineNumber, _charStream.ColumnNumber);
			}

			int startCol = _charStream.ColumnNumber;
			int startLine = _charStream.LineNumber;
			int startPos = _position;

			if(_state == LexerState.Base)
			{
				// Check for ${
				if(_charStream.HasChars(2))
				{
					var chars = _charStream.Peek(2);
					if(chars[0] == '$' && chars[1] == '{')
					{
						NextChar(); NextChar();
						_state = LexerState.Placeholder;
						return new Token(TokenType.DollarBraceOpen, "${", startPos, startLine, startCol);
					}
					if(chars[0] == '{' && chars[1] == '{')
					{
						NextChar(); NextChar();
						return new Token(TokenType.LiteralString, "{{", startPos, startLine, startCol);
					}
					if(chars[0] == '}' && chars[1] == '}')
					{
						NextChar(); NextChar();
						return new Token(TokenType.LiteralString, "}}", startPos, startLine, startCol);
					}
				}

				// Read text
				StringBuilder sb = new StringBuilder();
				while(!IsEOF())
				{
					// Check break conditions
					if(_charStream.HasChars(2))
					{
						var chars = _charStream.Peek(2);
						if(chars[0] == '$' && chars[1] == '{') break;
						if(chars[0] == '{' && chars[1] == '{') break;
						if(chars[0] == '}' && chars[1] == '}') break;
					}
					sb.Append(NextChar());
				}

				if(sb.Length > 0)
				{
					return new Token(TokenType.LiteralString, sb.ToString(), startPos, startLine, startCol);
				}
			} else if(_state == LexerState.Placeholder)
			{
				SkipWhitespace();

				// Fix: Check EOF before proceeding, but throw only if we really expected a token.
				// However, IsEOF() returns true if we are at end.
				// If we are in Placeholder state, EOF is an error (Unterminated).
				if(IsEOF())
				{
					throw new InvalidOperationException("Unterminated placeholder");
				}

				startCol = _charStream.ColumnNumber;
				startLine = _charStream.LineNumber;
				startPos = _position;

				char c = PeekNextChar();

				if(c == '}')
				{
					NextChar();
					_state = LexerState.Base;
					return new Token(TokenType.BraceClose, "}", startPos, startLine, startCol);
				}
				if(c == '.') { NextChar(); return new Token(TokenType.Dot, ".", startPos, startLine, startCol); }
				if(c == ',') { NextChar(); return new Token(TokenType.Comma, ",", startPos, startLine, startCol); }
				if(c == ':') { NextChar(); return new Token(TokenType.Colon, ":", startPos, startLine, startCol); }
				if(c == '(') { NextChar(); return new Token(TokenType.ParenOpen, "(", startPos, startLine, startCol); }
				if(c == ')') { NextChar(); return new Token(TokenType.ParenClose, ")", startPos, startLine, startCol); }
				if(c == '§') { NextChar(); return new Token(TokenType.Section, "§", startPos, startLine, startCol); }
				if(c == '?') { NextChar(); return new Token(TokenType.QuestionMark, "?", startPos, startLine, startCol); }

				if(_charStream.HasChars(2))
				{
					var chars = _charStream.Peek(2);
					if(chars[0] == '#' && chars[1] == '{')
					{
						NextChar(); NextChar();
						return new Token(TokenType.HashBraceOpen, "#{", startPos, startLine, startCol);
					}
				}

				if(char.IsDigit(c))
				{
					StringBuilder sb = new StringBuilder();
					while(!IsEOF() && char.IsDigit(PeekNextChar()))
					{
						sb.Append(NextChar());
					}
					return new Token(TokenType.LiteralInteger, sb.ToString(), startPos, startLine, startCol);
				}

				if(IsIdentifierStart(c))
				{
					StringBuilder sb = new StringBuilder();
					while(!IsEOF() && IsIdentifierPart(PeekNextChar()))
					{
						sb.Append(NextChar());
					}
					return new Token(TokenType.Identifier, sb.ToString(), startPos, startLine, startCol);
				}

				throw new InvalidOperationException($"Unexpected character: {c}");
			}

			throw new InvalidOperationException("Unexpected state");
		}

		public bool HasNextToken()
		{
			return _state != LexerState.EOF;
		}

		private bool IsIdentifierStart(char c)
		{
			return char.IsLetter(c) || c == '_' || c == '$' || c == '@';
		}
		private bool IsIdentifierPart(char c)
		{
			return char.IsLetterOrDigit(c) || c == '_' || c == '$' || c == '@' || c == '-';
		}

		public char NextChar()
		{
			_position++;
			return _charStream.Next();
		}
		public char PeekNextChar()
		{
			return _charStream.Peek();
		}

		public bool IsEOF()
		{
			return _charStream.EndOfStream;
		}

		private void SkipWhitespace()
		{
			while(!IsEOF() && char.IsWhiteSpace(_charStream.Peek()))
			{
				NextChar();
			}
		}
	}
}