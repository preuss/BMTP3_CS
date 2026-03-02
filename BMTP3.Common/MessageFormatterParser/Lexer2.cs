using System.Text;

namespace BMTP3.Common.MessageFormatterParser;

public class Lexer2 : ILexer
{
	private readonly CharStream _charStream;
	private LexerState _currentLexerState;
	private readonly System.Collections.Generic.Stack<LexerState> _stateStack = new();
	private bool _evalNextAsText = false;

	private enum LexerState
	{
		Default,
		InsideExpression,
		InsidePatternLiteral,
		InsideEvalPattern
	}

	public Lexer2(string input)
	{
		_charStream = new CharStream(input);
		_currentLexerState = LexerState.Default;
	}

	public Token NextToken()
	{
		if(!HasNextToken())
		{
			return new Token(TokenType.EOF, string.Empty, _charStream.ColumnNumber, _charStream.LineNumber, _charStream.ColumnNumber);
		}

		int startColumn = _charStream.ColumnNumber;


		if(_currentLexerState == LexerState.Default)
		{
			if(_charStream.HasChars(2))
			{
				List<char> twoChars = _charStream.Peek(2);
				if(twoChars[0] == '$' && twoChars[1] == '{')
				{
					_charStream.Next();
					_charStream.Next();
					// Enter expression state and remember previous
					_stateStack.Push(_currentLexerState);
					_currentLexerState = LexerState.InsideExpression;
					return new Token(TokenType.DollarBraceOpen, "${", startColumn, _charStream.LineNumber, startColumn);
				} else if(twoChars[0] == '#' && twoChars[1] == '{')
				{
					// Consistently emit HashBraceOpen for "#{" (Index/Hash naming were inconsistent)
					_charStream.Next();
					_charStream.Next();
					// Enter expression state and remember previous
					_stateStack.Push(_currentLexerState);
					_currentLexerState = LexerState.InsideExpression;
					return new Token(TokenType.HashBraceOpen, "#{", startColumn, _charStream.LineNumber, startColumn);
				}
			}

			return ScanLiteralString();
		} else if(_currentLexerState == LexerState.InsideExpression)
		{
			// Skip optional whitespace after an opening brace for normal expressions
			RemoveWhitespaces();
			startColumn = _charStream.ColumnNumber;

			char peekNextChar = _charStream.Peek();

			if(peekNextChar == '}')
			{
				_charStream.Next();
				// Restore previous lexer state if available
				if(_stateStack.Count > 0)
					_currentLexerState = _stateStack.Pop();
				else
					_currentLexerState = LexerState.Default;
				return new Token(TokenType.BraceClose, "}", startColumn, _charStream.LineNumber, startColumn);
			} else if(peekNextChar == '.')
			{
				_charStream.Next();
				return new Token(TokenType.Dot, ".", startColumn, _charStream.LineNumber, startColumn);
			} else if(peekNextChar == ',')
			{
				_charStream.Next();
				return new Token(TokenType.Comma, ",", startColumn, _charStream.LineNumber, startColumn);
			} else if(peekNextChar == ':')
			{
				_charStream.Next();
				// Enter pattern literal state and remember previous
				_stateStack.Push(_currentLexerState);
				_currentLexerState = LexerState.InsidePatternLiteral;
				return new Token(TokenType.Colon, ":", startColumn, _charStream.LineNumber, startColumn);
			} else if(peekNextChar == '(')
			{
				_charStream.Next();
				return new Token(TokenType.ParenOpen, "(", startColumn, _charStream.LineNumber, startColumn);
			} else if(peekNextChar == ')')
			{
				_charStream.Next();
				return new Token(TokenType.ParenClose, ")", startColumn, _charStream.LineNumber, startColumn);
			} else if(peekNextChar == '§' || peekNextChar == '¶')
			{
				// Enter eval pattern state and remember previous
				_stateStack.Push(_currentLexerState);
				_currentLexerState = LexerState.InsideEvalPattern;
				_evalNextAsText = false;
				return new Token(TokenType.Section, _charStream.Next(), startColumn, _charStream.LineNumber, startColumn);
			}

			// Allow quoted string arguments within function calls inside expressions
			if(peekNextChar == '`' || peekNextChar == '\'' || peekNextChar == '"' || peekNextChar == '´' || peekNextChar == '/')
			{
				return ScanLiteralStringInExpression();
			}

			return ScanIdentifier();
		} else if(_currentLexerState == LexerState.InsidePatternLiteral)
		{
			return ScanPatternLiteral();
		} else if(_currentLexerState == LexerState.InsideEvalPattern)
		{
			// Skip optional whitespace at start of eval sub-expression unless we're preserving branch text
			if(!_evalNextAsText)
				RemoveWhitespaces();
			startColumn = _charStream.ColumnNumber;

			if(!HasNextToken())
			{
				throw new InvalidOperationException("Unterminated eval pattern.");
			}

			char peekNextChar = _charStream.Peek();

			// If we were instructed to preserve branch text and the next char is whitespace,
			// consume the following run as eval text (preserve leading space)
			if(_evalNextAsText && char.IsWhiteSpace(peekNextChar))
			{
				_evalNextAsText = false;
				return ScanEvalText();
			}

			if(peekNextChar == '?')
			{
				return new Token(TokenType.QuestionMark, _charStream.Next(), startColumn, _charStream.LineNumber, startColumn);
			} else if(peekNextChar == ':')
			{
				return new Token(TokenType.Colon, _charStream.Next(), startColumn, _charStream.LineNumber, startColumn);
			} else if(peekNextChar == '`' || peekNextChar == '\'' || peekNextChar == '\"' || peekNextChar == '´' || peekNextChar == '/')
			{
				return ScanLiteralStringInExpression();
			} else if(char.IsLetter(peekNextChar))
			{
				// If a previous section indicated nested text parsing (e.g., we saw 'if,'), prefer text
				if(_evalNextAsText)
				{
					_evalNextAsText = false;
					return ScanEvalText();
				}

				// If the upcoming eval segment contains a space before any delimiter, treat as a literal text
				var look = LookAheadUntil(':', '}', '?', ',');
				// If the lookahead contains a space but no digits, it's likely branch text like 'No files'
				if(look.Contains(' ') && !look.Any(char.IsDigit))
					return ScanEvalText();
				return ScanIdentifier();
			} else if(char.IsDigit(peekNextChar))
			{
				// ensure leading whitespace isn't included in numeric token
				RemoveWhitespaces();
				return ScanLiteralInteger();
			} else if(peekNextChar == '}')
			{
				_charStream.Next();
				// Restore previous lexer state when closing eval pattern
				if(_stateStack.Count > 0)
					_currentLexerState = _stateStack.Pop();
				else
					_currentLexerState = LexerState.Default;
				return new Token(TokenType.BraceClose, "}", startColumn, _charStream.LineNumber, startColumn);
			} else if(peekNextChar == ',')
			{
				return new Token(TokenType.Comma, _charStream.Next(), startColumn, _charStream.LineNumber, startColumn);
			} else if(_charStream.HasChars(2) && _charStream.Peek(2)[0] == '#' && _charStream.Peek(2)[1] == '{')
			{
				_charStream.Next();
				_charStream.Next();
				// Enter nested expression state and remember previous
				_stateStack.Push(_currentLexerState);
				_currentLexerState = LexerState.InsideExpression;
				return new Token(TokenType.HashBraceOpen, "#{", startColumn, _charStream.LineNumber, startColumn);
			} else if(_charStream.HasChars(2) && _charStream.Peek(2)[0] == '$' && _charStream.Peek(2)[1] == '{')
			{
				_charStream.Next();
				_charStream.Next();
				// Enter nested expression state and remember previous
				_stateStack.Push(_currentLexerState);
				_currentLexerState = LexerState.InsideExpression;
				return new Token(TokenType.DollarBraceOpen, "${", startColumn, _charStream.LineNumber, startColumn);
			}

			throw new InvalidOperationException($"Invalid character in eval pattern: '{peekNextChar}'");

			// Standard for scanning of identification for eval-pattern (ex. "eq0" or "low")
			//return ScanPattern();
		}

		throw new InvalidOperationException("Unhandled lexer state or input.");
	}

	// Scan a literal string from the default lexer state. This consumes any characters
	// until an expression open sequence ('${' or '#{') is encountered or until EOF.
	private Token ScanLiteralString()
	{
		int startColumn = _charStream.ColumnNumber;
		StringBuilder buffer = new();

		while(HasNextToken())
		{
			char peekNextChar = _charStream.Peek();
			if(_charStream.HasChars(2))
			{
				char peekNextNextChar = _charStream.Peek(2)[1];

				// Checks if expression starts and end of literal string.
				if(peekNextChar == '$')
				{
					if(peekNextNextChar == '{')
					{
						break;
					}
				} else if(peekNextChar == '#')
				{
					if(peekNextNextChar == '{')
					{
						break;
					}
				}

				// Checks for escape sequences
				if(peekNextChar == '{')
				{
					// Handle escaping open brace
					if(peekNextNextChar == '{')
					{
						_charStream.Next();
						buffer.Append(_charStream.Next());
						continue;
					}
				} else if(peekNextChar == '}')
				{
					// Handle escaping close brace
					if(peekNextNextChar == '}')
					{
						_charStream.Next();
						buffer.Append(_charStream.Next());
						continue;
					}
				}
			}
			buffer.Append(_charStream.Next());

		}

		if(buffer.Length == 0)
		{
			if(_charStream.EndOfStream)
			{
				throw new IndexOutOfRangeException("It should never could try to ScanLiteralString if EOF.");
			} else
			{
				throw new InvalidOperationException("Problem no EOF but still no literal string");
			}
		}
		return new Token(TokenType.LiteralString, buffer.ToString(), startColumn, _charStream.LineNumber, startColumn);
	}
	private Token ScanLiteralInteger()
	{
		int startColumn = _charStream.ColumnNumber;
		StringBuilder buffer = new StringBuilder();

		while(HasNextToken() && char.IsDigit(_charStream.Peek()))
		{
			buffer.Append(_charStream.Next());
		}

		if(buffer.Length == 0)
		{
			throw new InvalidOperationException("Expected an integer but found none.");
		}

		return new Token(TokenType.LiteralInteger, buffer.ToString(), startColumn, _charStream.LineNumber, startColumn);
	}

	// Scan a pattern literal when inside a pattern (after a ':'). This stops when a '}' is reached
	// but will not consume nested expression openings so they can be tokenized separately.
	private Token ScanPattern()
	{
		if(_currentLexerState != LexerState.InsideEvalPattern)
		{
			throw new InvalidOperationException("Lexer is not in valid pattern state.");
		}

		if(_charStream.EndOfStream) throw new IndexOutOfRangeException("No pattern.");

		int startColumn = _charStream.ColumnNumber;

		StringBuilder buffer = new StringBuilder();

		while(HasNextToken())
		{
			char peekFirstChar = _charStream.Peek();
			if('}' == peekFirstChar)
			{
				if(_charStream.HasChars(2) && '}' == _charStream.Peek(2)[1])
				{
					_charStream.Next(); // Escape
					buffer.Append(_charStream.Next());
					continue;
				} else
				{
					// End of expression
					break;
				}
			}
			buffer.Append(_charStream.Next());
		}

		return new Token(TokenType.LiteralPattern, buffer.ToString(), startColumn, _charStream.LineNumber, startColumn);
	}
	private Token ScanLiteralStringInExpression()
	{
		int startColumn = _charStream.ColumnNumber;
		StringBuilder buffer = new StringBuilder();

		char delimiter = _charStream.Next();
		while(HasNextToken() && _charStream.Peek() != delimiter)
		{
			if(_charStream.Peek() == '\\' && _charStream.HasChars(2) && _charStream.Peek(2)[1] == delimiter)
			{
				_charStream.Next();
				buffer.Append(_charStream.Next());
				continue;
			}
			buffer.Append(_charStream.Next());
		}

		if(!HasNextToken() || _charStream.Peek() != delimiter)
		{
			throw new InvalidOperationException("Unterminated string literal.");
		}

		_charStream.Next();
		return new Token(TokenType.LiteralString, buffer.ToString(), startColumn, _charStream.LineNumber, startColumn);
	}

	private Token ScanEvalText()
	{
		int startColumn = _charStream.ColumnNumber;
		StringBuilder buffer = new StringBuilder();

		while(HasNextToken())
		{
			char c = _charStream.Peek();
			// stop on delimiters or nested expressions
			if(c == '?' || c == ':' || c == '}' || c == ',') break;
			if(_charStream.HasChars(2))
			{
				var two = _charStream.Peek(2);
				if((two[0] == '$' || two[0] == '#') && two[1] == '{') break;
			}
			buffer.Append(_charStream.Next());
		}

		// Preserve whitespace inside eval branch texts so adjacent placeholders/literals keep spacing
		return new Token(TokenType.LiteralString, buffer.ToString(), startColumn, _charStream.LineNumber, startColumn);
	}

	private string LookAheadUntil(params char[] delimiters)
	{
		var sb = new System.Text.StringBuilder();
		int i = 1;
		while(true)
		{
			if(!_charStream.HasChars(i)) break;
			var list = _charStream.Peek(i);
			char c = list[list.Count - 1];
			if(delimiters.Contains(c)) break;
			sb.Append(c);
			i++;
			if(i > 512) break; // safety
		}
		return sb.ToString();
	}
	private Token ScanIdentifier()
	{
		/*
		if(_currentLexerState != LexerState.InsideExpression) {
			throw new InvalidOperationException("Lexer is not in valid identifier state.");
		}*/

		/*
		if(_charStream.EndOfStream) throw new IndexOutOfRangeException("Not identifier.");
		*/

		int startColumn = _charStream.ColumnNumber;

		StringBuilder buffer = new StringBuilder();

		char peekFirstChar = _charStream.Peek();
		if(IsLegalIdentifierFirstCharacter(peekFirstChar))
		{
			buffer.Append(_charStream.Next());
		} else
		{
			throw new InvalidDataException($"Next char is illegal identifier '{peekFirstChar}'");
		}


		while(HasNextToken() && IsLegalIdentifierCharacter(_charStream.Peek()))
		{
			buffer.Append(_charStream.Next());
		}

		if(!HasNextToken())
		{
			throw new InvalidOperationException("Invalid identifier, because not end message formatter");
		}
		return new Token(TokenType.Identifier, buffer.ToString(), startColumn, _charStream.LineNumber, startColumn);
	}

	private bool IsLegalIdentifierFirstCharacter(char firstCharacter)
	{
		return (firstCharacter >= 'a' && firstCharacter <= 'z') ||
			   (firstCharacter >= 'A' && firstCharacter <= 'Z') ||
			   firstCharacter == '$' ||
			   firstCharacter == '@' ||
			   firstCharacter == '_';
	}

	private bool IsLegalIdentifierCharacter(char character)
	{
		return IsLegalIdentifierFirstCharacter(character) ||
			   (character >= '0' && character <= '9');
	}

	private Token ScanPatternLiteral()
	{
		int startColumn = _charStream.ColumnNumber;
		StringBuilder buffer = new StringBuilder();

		while(HasNextToken() && _charStream.Peek() != '}')
		{
			// If a nested expression starts inside the pattern (e.g. '${' or '#{'), stop here so the
			// expression can be tokenized separately. Do not consume the '${'/'#{'.
			if(_charStream.HasChars(2))
			{
				var two = _charStream.Peek(2);
				if((two[0] == '$' || two[0] == '#') && two[1] == '{')
				{
					break;
				}
			}

			buffer.Append(_charStream.Next());
		}

		_currentLexerState = LexerState.InsideExpression;
		return new Token(TokenType.LiteralPattern, buffer.ToString(), startColumn, _charStream.LineNumber, startColumn);
	}

	private void RemoveWhitespaces()
	{
		while(HasNextToken() && char.IsWhiteSpace(_charStream.Peek()))
		{
			_charStream.Next();
		}
	}

	public bool HasNextToken()
	{
		return !_charStream.EndOfStream;
	}
}
