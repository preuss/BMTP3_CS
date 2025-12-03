using BMTP3.Common.MessageFormatterParser;

namespace BMTP3.Common.Tests.MessageFormatterParser
{
	public class LexerTests
	{
		private void AssertTokens(List<Token> expected, List<Token> actual)
		{
			Assert.Equal(expected.Count, actual.Count);
			for(int i = 0; i < expected.Count; i++)
			{
				Assert.Equal(expected[i].Type, actual[i].Type);
				Assert.Equal(expected[i].Value, actual[i].Value);
				Assert.Equal(expected[i].Position, actual[i].Position);
			}
		}

		[Fact]
		public void ScanToken_SimpleInput_ReturnsCorrectTokens()
		{
			// Arrange
			var lexer = new Lexer("some text ${ name }");
			var expectedTokens = new List<Token>
			{
				new Token(TokenType.LiteralString, "some text ", 0, 0, 0),
				new Token(TokenType.DollarBraceOpen, "${", 10, 0, 0),
				new Token(TokenType.Identifier, "name", 13, 0, 0),
				new Token(TokenType.BraceClose, "}", 18, 0, 0),
				new Token(TokenType.EOF, "", 19, 0, 0)
			};
			var actualTokens = new List<Token>();

			// Act
			while(lexer.HasNextToken())
			{
				actualTokens.Add(lexer.NextToken());
			}

			// Assert
			AssertTokens(expectedTokens, actualTokens);
		}

		[Fact]
		public void ScanToken_ComplexInput_ReturnsCorrectTokens()
		{
			// Arrange
			var lexer = new Lexer("${date.toUpper(),number:yyyy-MM-dd}");
			var expectedTokens = new List<Token>
			{
				new Token(TokenType.DollarBraceOpen, "${", 0, 0, 0),
				new Token(TokenType.Identifier, "date", 2, 0, 0),
				new Token(TokenType.Dot, ".", 6, 0, 0),
				new Token(TokenType.Identifier, "toUpper", 7, 0, 0),
				new Token(TokenType.ParenOpen, "(", 14, 0, 0),
				new Token(TokenType.ParenClose, ")", 15, 0, 0),
				new Token(TokenType.Comma, ",", 16, 0, 0),
				new Token(TokenType.Identifier, "number", 17, 0, 0),
				new Token(TokenType.Colon, ":", 23, 0, 0),
				new Token(TokenType.Identifier, "yyyy-MM-dd", 24, 0, 0),
				new Token(TokenType.BraceClose, "}", 34, 0, 0),
				new Token(TokenType.EOF, "", 35, 0, 0)
			};
			var actualTokens = new List<Token>();

			// Act
			while(lexer.HasNextToken())
			{
				actualTokens.Add(lexer.NextToken());
			}

			// Assert
			AssertTokens(expectedTokens, actualTokens);
		}

		[Fact]
		public void ScanToken_EvalPattern_ReturnsCorrectTokens()
		{
			// Arrange
			var lexer = new Lexer("${count§eq0,0#{low}}");
			var expectedTokens = new List<Token>
			{
				new Token(TokenType.DollarBraceOpen, "${", 0, 0, 0),
				new Token(TokenType.Identifier, "count", 2, 0, 0),
				new Token(TokenType.Section, "§", 7, 0, 0),
				new Token(TokenType.Identifier, "eq0", 8, 0, 0),
				new Token(TokenType.Comma, ",", 11, 0, 0),
				new Token(TokenType.LiteralInteger, "0", 12, 0, 0),
				new Token(TokenType.HashBraceOpen, "#{", 13, 0, 0),
												new Token(TokenType.Identifier, "low", 15, 0, 0),
												new Token(TokenType.BraceClose, "}", 18, 0, 0),
												new Token(TokenType.LiteralString, "}", 19, 0, 0),
												new Token(TokenType.EOF, "", 20, 0, 0)
											}; var actualTokens = new List<Token>();

			// Act
			while(lexer.HasNextToken())
			{
				actualTokens.Add(lexer.NextToken());
			}

			// Assert
			AssertTokens(expectedTokens, actualTokens);
		}

		[Fact]
		public void ScanToken_EscapedBraces_ReturnsCorrectTokens()
		{
			// Arrange
			var lexer = new Lexer("text {{escaped}} text");
			var expectedTokens = new List<Token>
			{
				new Token(TokenType.LiteralString, "text ", 0, 0, 0),
				new Token(TokenType.LiteralString, "{{", 5, 0, 0),
				new Token(TokenType.LiteralString, "escaped", 7, 0, 0),
				new Token(TokenType.LiteralString, "}}", 14, 0, 0),
				new Token(TokenType.LiteralString, " text", 16, 0, 0),
				new Token(TokenType.EOF, "", 21, 0, 0)
			};
			var actualTokens = new List<Token>();

			// Act
			while(lexer.HasNextToken())
			{
				actualTokens.Add(lexer.NextToken());
			}

			// Assert
			AssertTokens(expectedTokens, actualTokens);
		}

		[Fact]
		public void ScanToken_PlainText_ReturnsSimpleSingleLiteralString()
		{
			// Arrange
			var lexer = new Lexer("hello");
			var expectedTokens = new List<Token>
			{
				new Token(TokenType.LiteralString, "hello", 0, 0, 0),
				new Token(TokenType.EOF, "", 5, 0, 0)
			};
			var actualTokens = new List<Token>();

			// Act
			while(lexer.HasNextToken())
			{
				actualTokens.Add(lexer.NextToken());
			}

			// Assert
			AssertTokens(expectedTokens, actualTokens);
		}
		[Fact]
		public void ScanToken_PlainText_ReturnsSingleLiteralString()
		{
			// Arrange
			var lexer = new Lexer("hello world");
			var expectedTokens = new List<Token>
			{
				new Token(TokenType.LiteralString, "hello world", 0, 0, 0),
				new Token(TokenType.EOF, "", 11, 0, 0)
			};
			var actualTokens = new List<Token>();

			// Act
			while(lexer.HasNextToken())
			{
				actualTokens.Add(lexer.NextToken());
			}

			// Assert
			AssertTokens(expectedTokens, actualTokens);
		}

		[Fact]
		public void ScanToken_LoneDollarOrHash_ReturnsLiteralString()
		{
			// Arrange
			var lexer = new Lexer("text $ #");
			var expectedTokens = new List<Token>
			{
				new Token(TokenType.LiteralString, "text $ #", 0, 0, 0),
				new Token(TokenType.EOF, "", 8, 0, 0)
			};
			var actualTokens = new List<Token>();

			// Act
			while(lexer.HasNextToken())
			{
				actualTokens.Add(lexer.NextToken());
			}

			// Assert
			AssertTokens(expectedTokens, actualTokens);
		}

		[Fact]
		public void ScanToken_EmptyInput_ReturnsEOF()
		{
			// Arrange
			var lexer = new Lexer("");
			var expectedTokens = new List<Token>
			{
				new Token(TokenType.EOF, "", 0, 0, 0)
			};
			var actualTokens = new List<Token>();

			// Act
			while(lexer.HasNextToken())
			{
				actualTokens.Add(lexer.NextToken());
			}

			// Assert
			AssertTokens(expectedTokens, actualTokens);
		}

		[Fact]
		public void ScanToken_UnterminatedPlaceholder_ThrowsException()
		{
			// Arrange
			var lexer = new Lexer("${name");

			// Act & Assert
			Assert.Throws<InvalidOperationException>(() =>
			{
				while(lexer.HasNextToken())
				{
					lexer.NextToken();
				}
			});
		}

		[Fact]
		public void PeekNextChar_AtEOF_ThrowsException()
		{
			// Arrange
			var lexer = new Lexer("");
			while(lexer.HasNextToken())
			{
				lexer.NextToken(); // Consume EOF
			}

			// Act & Assert
			// Dette vil nu kaste en CharStreamException, ikke InvalidOperationException
			Assert.Throws<CharStreamException>(() => lexer.PeekNextChar());
		}

		[Fact]
		public void ConsumeNextChar_AtEOF_ThrowsException()
		{
			// Arrange
			var lexer = new Lexer("");
			while(lexer.HasNextToken())
			{
				lexer.NextToken(); // Consume EOF
			}

			// Act & Assert
			// Dette vil nu kaste en CharStreamException, ikke InvalidOperationException
			Assert.Throws<CharStreamException>(() => lexer.NextChar());
		}

		[Fact]
		public void HasMoreTokens_ReturnsFalseAfterEOF()
		{
			// Arrange
			var lexer = new Lexer("text");

			// Act
			while(lexer.HasNextToken())
			{
				lexer.NextToken();
			}

			// Assert
			Assert.False(lexer.HasNextToken());
		}
	}
}