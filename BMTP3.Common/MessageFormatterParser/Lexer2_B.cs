using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Common.MessageFormatterParser;

public class Lexer2_B : ILexer {
	private readonly CharStream _charStream;
	private LexerState _currentLexerState;

	// Enum for at definere lexerens tilstande
	private enum LexerState {
		Default,
		InsideExpression,
		InsidePatternLiteral
	}

	public Lexer2_B(string input) {
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
			/*
			StringBuilder buffer = new();
			while(HasNextToken()) {
				char peekNextChar = _charStream.Peek();
				if(_charStream.HasChars(2)) {
					char peekNextNextChar = _charStream.Peek(2)[1];
					if(peekNextChar == '{') {
						if(peekNextNextChar == '{') {
							// Handle escaping open brace
							_charStream.Next();
							buffer.Append(_charStream.Next());
							continue;
						}
					} else if(peekNextChar == '}') {
						if(peekNextNextChar == '}') {
							// Handle escaping close brace
							_charStream.Next();
							buffer.Append(_charStream.Next());
							continue;
						}
					} else if(peekNextChar == '$') {
						if(peekNextNextChar == '{') {
							break;
						}
					}
				}

				buffer.Append(_charStream.Next());
			}

			return new Token(tokenType, buffer.ToString(), startColumn);
			*/
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
				return new Token(TokenType.Colon, ":", startColumn);
			} else if(peekNextChar == '(') {
				_charStream.Next();
				return new Token(TokenType.ParenOpen, "(", startColumn);
			} else if(peekNextChar == ')') {
				_charStream.Next();
				return new Token(TokenType.ParenClose, ")", startColumn);
			} else if(peekNextChar == ':') {
				_charStream.Next();
				_currentLexerState = LexerState.InsidePatternLiteral;
				return new Token(TokenType.Colon, ":", startColumn);
			}

			string identifier = ScanIdentifier();
			Token identifierToken = new Token(TokenType.Identifier, identifier, startColumn);
			return identifierToken;
		} else if(_currentLexerState == LexerState.InsidePatternLiteral) {
			return ScanPatternLiteral();
		}

		throw new InvalidOperationException("Unhandled lexer state or input.");
	}

	private Token ScanLiteralString() {
		StringBuilder buffer = new StringBuilder();
		int startColumn = _charStream.ColumnNumber;

		while(HasNextToken()) {
			// Checks for placeholders
			if(_charStream.HasChars(2) && (_charStream.Peek(2)[0] == '$' || _charStream.Peek(2)[0] == '#') && _charStream.Peek(2)[1] == '{') {
				break;
			}
			// Checks for escape sequences
			if(_charStream.HasChars(2) && (_charStream.Peek(2)[0] == '{' && _charStream.Peek(2)[1] == '{')) {
				_charStream.Next();
				_charStream.Next();
				buffer.Append('{');
			} else if(_charStream.HasChars(2) && (_charStream.Peek(2)[0] == '}' && _charStream.Peek(2)[1] == '}')) {
				_charStream.Next();
				_charStream.Next();
				buffer.Append('}');
			} else {
				buffer.Append(_charStream.Next());
			}
		}

		if(buffer.Length == 0) {
			if(_charStream.EndOfStream) {
				return new Token(TokenType.EOF, string.Empty, startColumn);
			} else {
				char singleChar = _charStream.Next();
				return new Token(TokenType.LiteralString, singleChar.ToString(), startColumn);
			}
		}

		return new Token(TokenType.LiteralString, buffer.ToString(), startColumn);
	}
	private string ScanIdentifier() {
		if(_currentLexerState != LexerState.InsideExpression) {
			throw new InvalidOperationException("Lexer is not in inside expression state");
		}

		if(_charStream.EndOfStream) throw new IndexOutOfRangeException("Not identifier.");

		StringBuilder buffer = new StringBuilder();

		char peekFirstChar = _charStream.Peek();
		if(LegalIdentifierFirstCharacter(peekFirstChar)) {
			buffer.Append(_charStream.Next());
		} else {
			throw new InvalidDataException($"Next char is illegal identifier '{peekFirstChar}'");
		}

		char peekNextChar;
		while(HasNextToken() && LegalIdentifierCharacter(_charStream.Peek())) {
			buffer.Append(_charStream.Next());
		}

		return buffer.ToString();
	}

	private bool LegalIdentifierFirstCharacter(char firstCharacter) {
		return (firstCharacter >= 'a' && firstCharacter <= 'z') ||
		       (firstCharacter >= 'A' && firstCharacter <= 'Z') ||
		       firstCharacter == '$' ||
		       firstCharacter == '@' ||
		       firstCharacter == '_';
	}

	private bool LegalIdentifierCharacter(char character) {
		return LegalIdentifierFirstCharacter(character) ||
		       (character >= '0' && character <= '9');
	}
	private Token ScanPatternLiteral() {
		int startColumn = _charStream.ColumnNumber;
		StringBuilder buffer = new StringBuilder();

		while(HasNextToken() && _charStream.Peek() != '}') {
			if(_charStream.HasChars(2) && (_charStream.Peek(2)[0] == '$' || _charStream.Peek(2)[0] == '#') && _charStream.Peek(2)[1] == '{') {
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