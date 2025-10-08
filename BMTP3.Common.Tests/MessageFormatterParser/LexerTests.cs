using BMTP3.Common.MessageFormatterParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Common.Tests.MessageFormatterParser {
	public class LexerTests {
		private void AssertTokens(List<Token> expected, List<Token> actual) {
			Assert.Equal(expected.Count, actual.Count);
			for(int i = 0; i < expected.Count; i++) {
				Assert.Equal(expected[i].Type, actual[i].Type);
				Assert.Equal(expected[i].Value, actual[i].Value);
				Assert.Equal(expected[i].Position, actual[i].Position);
			}
		}

		[Fact]
		public void ScanToken_SimpleInput_ReturnsCorrectTokens() {
			// Arrange
			var lexer = new Lexer("some text ${ name }");
			var expectedTokens = new List<Token>
			{
				new Token(TokenType.LiteralString, "some text ", 0),
				new Token(TokenType.DollarBraceOpen, "${", 10),
				new Token(TokenType.Identifier, "name", 13),
				new Token(TokenType.BraceClose, "}", 18),
				new Token(TokenType.EOF, "", 19)
			};
			var actualTokens = new List<Token>();

			// Act
			while(lexer.HasNextToken()) {
				actualTokens.Add(lexer.NextToken());
			}

			// Assert
			AssertTokens(expectedTokens, actualTokens);
		}

		[Fact]
		public void ScanToken_ComplexInput_ReturnsCorrectTokens() {
			// Arrange
			var lexer = new Lexer("${date.toUpper(),number:yyyy-MM-dd}");
			var expectedTokens = new List<Token>
			{
				new Token(TokenType.DollarBraceOpen, "${", 0),
				new Token(TokenType.Identifier, "date", 2),
				new Token(TokenType.Dot, ".", 6),
				new Token(TokenType.Identifier, "toUpper", 7),
				new Token(TokenType.ParenOpen, "(", 14),
				new Token(TokenType.ParenClose, ")", 15),
				new Token(TokenType.Comma, ",", 16),
				new Token(TokenType.Identifier, "number", 17),
				new Token(TokenType.Colon, ":", 23),
				new Token(TokenType.Identifier, "yyyy-MM-dd", 24),
				new Token(TokenType.BraceClose, "}", 34),
				new Token(TokenType.EOF, "", 35)
			};
			var actualTokens = new List<Token>();

			// Act
			while(lexer.HasNextToken()) {
				actualTokens.Add(lexer.NextToken());
			}

			// Assert
			AssertTokens(expectedTokens, actualTokens);
		}

		[Fact]
		public void ScanToken_EvalPattern_ReturnsCorrectTokens() {
			// Arrange
			var lexer = new Lexer("${count§eq0,0#low}");
			var expectedTokens = new List<Token>
			{
				new Token(TokenType.DollarBraceOpen, "${", 0),
				new Token(TokenType.Identifier, "count", 2),
				new Token(TokenType.Section, "§", 7),
				new Token(TokenType.Identifier, "eq0", 8),
				new Token(TokenType.Comma, ",", 11),
				new Token(TokenType.LiteralInteger, "0", 12),
				new Token(TokenType.HashBraceOpen, "#{", 13),
				new Token(TokenType.Identifier, "low", 15),
				new Token(TokenType.BraceClose, "}", 18),
				new Token(TokenType.EOF, "", 19)
			};
			var actualTokens = new List<Token>();

			// Act
			while(lexer.HasNextToken()) {
				actualTokens.Add(lexer.NextToken());
			}

			// Assert
			AssertTokens(expectedTokens, actualTokens);
		}

		[Fact]
		public void ScanToken_EscapedBraces_ReturnsCorrectTokens() {
			// Arrange
			var lexer = new Lexer("text {{escaped}} text");
			var expectedTokens = new List<Token>
			{
				new Token(TokenType.LiteralString, "text ", 0),
				new Token(TokenType.LiteralString, "{{", 5),
				new Token(TokenType.LiteralString, "escaped", 7),
				new Token(TokenType.LiteralString, "}}", 14),
				new Token(TokenType.LiteralString, " text", 16),
				new Token(TokenType.EOF, "", 21)
			};
			var actualTokens = new List<Token>();

			// Act
			while(lexer.HasNextToken()) {
				actualTokens.Add(lexer.NextToken());
			}

			// Assert
			AssertTokens(expectedTokens, actualTokens);
		}

		[Fact]
		public void ScanToken_PlainText_ReturnsSimpleSingleLiteralString() {
			// Arrange
			var lexer = new Lexer("hello");
			var expectedTokens = new List<Token>
			{
				new Token(TokenType.LiteralString, "hello", 0),
				new Token(TokenType.EOF, "", 5)
			};
			var actualTokens = new List<Token>();

			// Act
			while(lexer.HasNextToken()) {
				actualTokens.Add(lexer.NextToken());
			}

			// Assert
			AssertTokens(expectedTokens, actualTokens);
		}
		[Fact]
		public void ScanToken_PlainText_ReturnsSingleLiteralString() {
			// Arrange
			var lexer = new Lexer("hello world");
			var expectedTokens = new List<Token>
			{
				new Token(TokenType.LiteralString, "hello world", 0),
				new Token(TokenType.EOF, "", 11)
			};
			var actualTokens = new List<Token>();

			// Act
			while(lexer.HasNextToken()) {
				actualTokens.Add(lexer.NextToken());
			}

			// Assert
			AssertTokens(expectedTokens, actualTokens);
		}

		[Fact]
		public void ScanToken_LoneDollarOrHash_ReturnsLiteralString() {
			// Arrange
			var lexer = new Lexer("text $ #");
			var expectedTokens = new List<Token>
			{
				new Token(TokenType.LiteralString, "text $ #", 0),
				new Token(TokenType.EOF, "", 8)
			};
			var actualTokens = new List<Token>();

			// Act
			while(lexer.HasNextToken()) {
				actualTokens.Add(lexer.NextToken());
			}

			// Assert
			AssertTokens(expectedTokens, actualTokens);
		}

		[Fact]
		public void ScanToken_EmptyInput_ReturnsEOF() {
			// Arrange
			var lexer = new Lexer("");
			var expectedTokens = new List<Token>
			{
				new Token(TokenType.EOF, "", 0)
			};
			var actualTokens = new List<Token>();

			// Act
			while(lexer.HasNextToken()) {
				actualTokens.Add(lexer.NextToken());
			}

			// Assert
			AssertTokens(expectedTokens, actualTokens);
		}

		[Fact]
		public void ScanToken_UnterminatedPlaceholder_ThrowsException() {
			// Arrange
			var lexer = new Lexer("${name");

			// Act & Assert
			Assert.Throws<InvalidOperationException>(() => {
				while(lexer.HasNextToken()) {
					lexer.NextToken();
				}
			});
		}

		[Fact]
		public void PeekNextChar_AtEOF_ThrowsException() {
			// Arrange
			var lexer = new Lexer("");
			while(lexer.HasNextToken()) {
				lexer.NextToken(); // Consume EOF
			}

			// Act & Assert
			// Dette vil nu kaste en CharStreamException, ikke InvalidOperationException
			Assert.Throws<CharStreamException>(() => lexer.PeekNextChar());
		}

		[Fact]
		public void ConsumeNextChar_AtEOF_ThrowsException() {
			// Arrange
			var lexer = new Lexer("");
			while(lexer.HasNextToken()) {
				lexer.NextToken(); // Consume EOF
			}

			// Act & Assert
			// Dette vil nu kaste en CharStreamException, ikke InvalidOperationException
			Assert.Throws<CharStreamException>(() => lexer.NextChar());
		}

		[Fact]
		public void HasMoreTokens_ReturnsFalseAfterEOF() {
			// Arrange
			var lexer = new Lexer("text");

			// Act
			while(lexer.HasNextToken()) {
				lexer.NextToken();
			}

			// Assert
			Assert.False(lexer.HasNextToken());
		}
	}
}