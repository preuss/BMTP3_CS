using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data.Common;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Common.MessageFormatterParser;

public class Lexer2 : ILexer {
	private readonly CharStream _charStream;
	private LexerState _currentLexerState;

	private enum LexerState {
		Default,
		InsideExpression,
		InsidePatternLiteral,
		InsideEvalPattern
	}

	public Lexer2(string input) {
		_charStream = new CharStream(input);
		_currentLexerState = LexerState.Default;
	}

	public Token NextToken() {
		if(!HasNextToken()) {
			return new Token(TokenType.EOF, string.Empty, _charStream.ColumnNumber);
		}

		int startColumn = _charStream.ColumnNumber;
		TokenType tokenType = TokenType.LiteralString;

		if(_currentLexerState == LexerState.Default) {
			if(_charStream.HasChars(2)) {
				List<char> twoChars = _charStream.Peek(2);
				if(twoChars[0] == '$' && twoChars[1] == '{') {
					_charStream.Next();
					_charStream.Next();
					_currentLexerState = LexerState.InsideExpression;
					return new Token(TokenType.DollarBraceOpen, "${", startColumn);
				} else if(twoChars[0] == '#' && twoChars[1] == '{') {
					_charStream.Next();
					_charStream.Next();
					_currentLexerState = LexerState.InsideExpression;
					return new Token(TokenType.IndexBraceOpen, "#{", startColumn);
				}
			}

			return ScanLiteralString();
		} else if(_currentLexerState == LexerState.InsideExpression) {
			RemoveWhitespaces();
			startColumn = _charStream.ColumnNumber;

			char peekNextChar = _charStream.Peek();

			if(peekNextChar == '}') {
				_charStream.Next();
				_currentLexerState = LexerState.Default;
				return new Token(TokenType.BraceClose, "}", startColumn);
			} else if(peekNextChar == '.') {
				_charStream.Next();
				return new Token(TokenType.Dot, ".", startColumn);
			} else if(peekNextChar == ',') {
				_charStream.Next();
				return new Token(TokenType.Comma, ",", startColumn);
			} else if(peekNextChar == ':') {
				_charStream.Next();
				_currentLexerState = LexerState.InsidePatternLiteral;
				return new Token(TokenType.Colon, ":", startColumn);
			} else if(peekNextChar == '(') {
				_charStream.Next();
				return new Token(TokenType.ParenOpen, "(", startColumn);
			} else if(peekNextChar == ')') {
				_charStream.Next();
				return new Token(TokenType.ParenClose, ")", startColumn);
			} else if(peekNextChar == '§' || peekNextChar == '¶') {
				_currentLexerState = LexerState.InsideEvalPattern;
				return new Token(TokenType.Section, _charStream.Next(), startColumn);
			}

			return ScanIdentifier();
		} else if(_currentLexerState == LexerState.InsidePatternLiteral) {
			return ScanPatternLiteral();
		} else if(_currentLexerState == LexerState.InsideEvalPattern) {
			RemoveWhitespaces();
			startColumn = _charStream.ColumnNumber;

			if(!HasNextToken()) {
				throw new InvalidOperationException("Unterminated eval pattern.");
			}

			char peekNextChar = _charStream.Peek();

			if(peekNextChar == '?') {
				return new Token(TokenType.QuestionMark, _charStream.Next(), startColumn);
			} else if(peekNextChar == ':') {
				return new Token(TokenType.Colon, _charStream.Next(), startColumn);
			} else if(peekNextChar == '`' || peekNextChar == '\'' || peekNextChar == '\"' || peekNextChar == '´' || peekNextChar == '/') {
				return ScanLiteralStringInExpression();
			} else if(char.IsLetter(peekNextChar)) {
				return ScanIdentifier();
			} else if(char.IsDigit(peekNextChar)) {
				return ScanLiteralInteger();
			} else if(peekNextChar == '}') {
				_charStream.Next();
				_currentLexerState = LexerState.Default;
				return new Token(TokenType.BraceClose, "}", startColumn);
			} else if(peekNextChar == ',') {
				return new Token(TokenType.Comma, _charStream.Next(), startColumn);
			} else if(_charStream.HasChars(2) && _charStream.Peek(2)[0] == '#' && _charStream.Peek(2)[1] == '{') {
				_charStream.Next();
				_charStream.Next();
				_currentLexerState = LexerState.InsideExpression;
				return new Token(TokenType.HashBraceOpen, "#{", startColumn);
			}

			throw new InvalidOperationException($"Invalid character in eval pattern: '{peekNextChar}'");

			// Standard for scanning of identification for eval-pattern (ex. "eq0" or "low")
			//return ScanPattern();
		}

		throw new InvalidOperationException("Unhandled lexer state or input.");
	}

	private Token ScanLiteralString() {
		int startColumn = _charStream.ColumnNumber;
		StringBuilder buffer = new();

		while(HasNextToken()) {
			char peekNextChar = _charStream.Peek();
			if(_charStream.HasChars(2)) {
				char peekNextNextChar = _charStream.Peek(2)[1];

				// Checks if expression starts and end of literal string.
				if(peekNextChar == '$') {
					if(peekNextNextChar == '{') {
						break;
					}
				} else if(peekNextChar == '#') {
					if(peekNextNextChar == '{') {
						break;
					}
				}

				// Checks for escape sequences
				if(peekNextChar == '{') {
					// Handle escaping open brace
					if(peekNextNextChar == '{') {
						_charStream.Next();
						buffer.Append(_charStream.Next());
						continue;
					}
				} else if(peekNextChar == '}') {
					// Handle escaping close brace
					if(peekNextNextChar == '}') {
						_charStream.Next();
						buffer.Append(_charStream.Next());
						continue;
					}
				}
			}
			buffer.Append(_charStream.Next());

		}

		if(buffer.Length == 0) {
			if(_charStream.EndOfStream) {
				throw new IndexOutOfRangeException("It should never could try to ScanLiteralString if EOF.");
			} else {
				throw new InvalidOperationException("Problem no EOF but still no literal string");
			}
		}

		return new Token(TokenType.LiteralString, buffer.ToString(), startColumn);
	}
	private Token ScanLiteralInteger() {
		int startColumn = _charStream.ColumnNumber;
		StringBuilder buffer = new StringBuilder();

		while(HasNextToken() && char.IsDigit(_charStream.Peek())) {
			buffer.Append(_charStream.Next());
		}

		if(buffer.Length == 0) {
			throw new InvalidOperationException("Expected an integer but found none.");
		}

		return new Token(TokenType.LiteralInteger, buffer.ToString(), startColumn);
	}

	private Token ScanPattern() {
		if(_currentLexerState != LexerState.InsideEvalPattern) {
			throw new InvalidOperationException("Lexer is not in valid pattern state.");
		}

		if(_charStream.EndOfStream) throw new IndexOutOfRangeException("No pattern.");

		int startColumn = _charStream.ColumnNumber;

		StringBuilder buffer = new StringBuilder();

		while(HasNextToken()) {
			char peekFirstChar = _charStream.Peek();
			if('}' == peekFirstChar) {
				if(_charStream.HasChars(2) && '}' == _charStream.Peek(2)[1]) {
					_charStream.Next(); // Escape
					buffer.Append(_charStream.Next());
					continue;
				} else {
					// End of expression
					break;
				}
			}
			buffer.Append(_charStream.Next());
		}

		return new Token(TokenType.LiteralPattern, buffer.ToString(), startColumn);
	}
	private Token ScanLiteralStringInExpression() {
		int startColumn = _charStream.ColumnNumber;
		StringBuilder buffer = new StringBuilder();

		char delimiter = _charStream.Next();
		while(HasNextToken() && _charStream.Peek() != delimiter) {
			if(_charStream.Peek() == '\\' && _charStream.HasChars(2) && _charStream.Peek(2)[1] == delimiter) {
				_charStream.Next();
				buffer.Append(_charStream.Next());
				continue;
			}
			buffer.Append(_charStream.Next());
		}

		if(!HasNextToken() || _charStream.Peek() != delimiter) {
			throw new InvalidOperationException("Unterminated string literal.");
		}

		_charStream.Next();
		return new Token(TokenType.LiteralString, buffer.ToString(), startColumn);
	}
	private Token ScanIdentifier() {
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
		if(IsLegalIdentifierFirstCharacter(peekFirstChar)) {
			buffer.Append(_charStream.Next());
		} else {
			throw new InvalidDataException($"Next char is illegal identifier '{peekFirstChar}'");
		}

		char peekNextChar;
		while(HasNextToken() && IsLegalIdentifierCharacter(_charStream.Peek())) {
			buffer.Append(_charStream.Next());
		}

		if(!HasNextToken()) {
			throw new InvalidOperationException("Invalid identifier, because not end message formatter");
		}
		return new Token(TokenType.Identifier, buffer.ToString(), startColumn);
	}

	private bool IsLegalIdentifierFirstCharacter(char firstCharacter) {
		return (firstCharacter >= 'a' && firstCharacter <= 'z') ||
		       (firstCharacter >= 'A' && firstCharacter <= 'Z') ||
		       firstCharacter == '$' ||
		       firstCharacter == '@' ||
		       firstCharacter == '_';
	}

	private bool IsLegalIdentifierCharacter(char character) {
		return IsLegalIdentifierFirstCharacter(character) ||
		       (character >= '0' && character <= '9');
	}

	private Token ScanPatternLiteral() {
		int startColumn = _charStream.ColumnNumber;
		StringBuilder buffer = new StringBuilder();

		while(HasNextToken() && _charStream.Peek() != '}') {
			if(_charStream.HasChars(2) && (_charStream.Peek(2)[0] == '$' || _charStream.Peek(2)[0] == '#') &&
			   _charStream.Peek(2)[1] == '{') {
				// TODO: Implement logic to handle string literal in pattern, now we read all chars.
			}

			buffer.Append(_charStream.Next());
		}

		_currentLexerState = LexerState.InsideExpression;
		return new Token(TokenType.LiteralPattern, buffer.ToString(), startColumn);
	}

	private void RemoveWhitespaces() {
		while(HasNextToken() && char.IsWhiteSpace(_charStream.Peek())) {
			_charStream.Next();
		}
	}

	public bool HasNextToken() {
		return !_charStream.EndOfStream;
	}
}