using System;
using System.Collections.Generic;
using System.Text;

namespace BMTP3.Common.MessageFormatterParser {
	public class Lexer : ILexer {
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
		private readonly StringBuilder _buffer;
		private int _tokenPosition;
		private bool _inPlaceholder;

		public Lexer(string input) {
			_charStream = new CharStream(input);
			_state = LexerState.Base;
			_buffer = new StringBuilder();
			_tokenPosition = 0;
			_inPlaceholder = false;
		}

		public Token NextToken() {
			if(_state == LexerState.EOF) {
				throw new InvalidOperationException("Cannot scan token after EOF");
			}
			if(IsEOF()) {
				_state = LexerState.EOF;
				return new Token(TokenType.EOF, string.Empty, _tokenPosition);
			}

			while(HasNextToken()) {
				char nextChar = PeekNextChar();
				switch(_state) {
					case LexerState.Base:
						if(nextChar == '$') {
							_state = LexerState.Dollar;
						} else {
							_state = LexerState.Text;
						}
						_buffer.Append(NextChar());
						continue;
					case LexerState.Text:
						if(nextChar == '$') {
							_state = LexerState.Dollar;
						}
						_buffer.Append(NextChar());
						continue;
					case LexerState.Dollar:
						if(nextChar == '{') {
							_state = LexerState.BraceOpen;
						}
						_buffer.Append(NextChar());
						continue;
					case LexerState.Hash:
					case LexerState.BraceOpen:
					case LexerState.Placeholder:
					case LexerState.EscapedBrace:
					case LexerState.EOF:
					default:
						throw new InvalidOperationException($"Invalid state: {_state}");
				}
			}

			// Find the right state to scan
			if(_state == LexerState.Base) {
				char peekChar = PeekNextChar();
				if(peekChar == '$') {
					_state = LexerState.Dollar;
				} else if(peekChar == '#') {
					_state = LexerState.Hash;
				} else {
					_state = LexerState.Text;
				}
			}

			_tokenPosition = _charStream.ColumnNumber;
			_buffer.Clear();
			while(!IsEOF()) {
				_buffer.Append(NextChar()); // Brug den nye NextChar metode
			}
			if(_buffer.Length > 0) {
				return new Token(TokenType.LiteralString, _buffer.ToString(), _tokenPosition);
			}

			_state = LexerState.EOF;
			return new Token(TokenType.EOF, string.Empty, _tokenPosition);
		}

		public bool HasNextToken() {
			return _state != LexerState.EOF;
		}

		public char NextChar() {
			return _charStream.Next();
		}
		public char PeekNextChar() {
			return _charStream.Peek();
		}

		public bool IsEOF() {
			return _charStream.EndOfStream;
		}

		private void SkipWhitespace() {
			while(!IsEOF() && char.IsWhiteSpace(_charStream.Peek())) { // Brug Peek
				NextChar(); // Brug NextChar
			}
		}

		private Token ScanInitial() {
			throw new NotImplementedException();
		}

		private Token ScanDollar() {
			throw new NotImplementedException();
		}

		private Token ScanHash() {
			throw new NotImplementedException();
		}

		private Token ScanBraceOpen() {
			throw new NotImplementedException();
		}

		private Token ScanEscapedBrace() {
			throw new NotImplementedException();
		}

		private Token ScanText() {
			throw new NotImplementedException();
		}
	}
}