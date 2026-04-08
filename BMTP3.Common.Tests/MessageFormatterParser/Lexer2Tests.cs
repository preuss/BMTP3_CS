using BMTP3.Common.MessageFormatterParser;

namespace BMTP3.Common.Tests.MessageFormatterParser;

public class Lexer2Tests
{
	private void AssertTokensx(List<Token> expected, List<Token> actual)
	{
		Assert.Equal(expected.Count, actual.Count);
		for (int i = 0; i < expected.Count; i++)
		{
			Assert.Equal(expected[i].Type, actual[i].Type);
			Assert.Equal(expected[i].Value, actual[i].Value);
			Assert.Equal(expected[i].Position, actual[i].Position);
		}
	}

	private void AssertTokens(List<Token> expected, List<Token> actual)
	{
		Assert.True(expected.Count == actual.Count, $"Expected {expected.Count} tokens but found {actual.Count}.");

		for (int i = 0; i < expected.Count; i++)
		{
			Token expectedToken = expected[i];
			Token actualToken = actual[i];

			Assert.True(expectedToken.Type == actualToken.Type,
				$"Token {i}: Expected Type '{expectedToken.Type}' but was '{actualToken.Type}'. " +
				$"Expected: {expectedToken} Actual: {actualToken}");

			Assert.True(expectedToken.Value == actualToken.Value,
				$"Token {i}: Expected Value '{expectedToken.Value}' but was '{actualToken.Value}'. " +
				$"Expected: {expectedToken} Actual: {actualToken}");

			Assert.True(expectedToken.Position == actualToken.Position,
				$"Token {i}: Expected Position '{expectedToken.Position}' but was '{actualToken.Position}'. " +
				$"Expected: {expectedToken} Actual: {actualToken}");
		}
	}

	[Fact]
	public void ScanToken_SimpleInput_ReturnsCorrectTokens()
	{
		// Arrange
		Lexer2 lexer = new("some text $x ${ name }");
		List<Token> expectedTokens =
		[
			new(TokenType.LiteralString, "some text $x ", 0, 0, 0),
			new(TokenType.DollarBraceOpen, "${", 13, 0, 0),
			new(TokenType.Identifier, "name", 16, 0, 0),
			new(TokenType.BraceClose, "}", 21, 0, 0),
			new(TokenType.EOF, "", 22, 0, 0)
		];
		List<Token> actualTokens = [];

		// Act
		while (lexer.HasNextToken())
		{
			actualTokens.Add(lexer.NextToken());
		}

		actualTokens.Add(lexer.NextToken());

		// Assert
		AssertTokens(expectedTokens, actualTokens);
	}

	[Fact]
	public void ScanToken_ComplexInput_ReturnsCorrectTokens()
	{
		// Arrange
		Lexer2 lexer = new("${date.toUpper(),date:yyyy-MM-dd}");
		List<Token> expectedTokens =
		[
			new(TokenType.DollarBraceOpen, "${", 0, 0, 0),
			new(TokenType.Identifier, "date", 2, 0, 0),
			new(TokenType.Dot, ".", 6, 0, 0),
			new(TokenType.Identifier, "toUpper", 7, 0, 0),
			new(TokenType.ParenOpen, "(", 14, 0, 0),
			new(TokenType.ParenClose, ")", 15, 0, 0),
			new(TokenType.Comma, ",", 16, 0, 0),
			new(TokenType.Identifier, "date", 17, 0, 0),
			new(TokenType.Colon, ":", 21, 0, 0),
			new(TokenType.LiteralPattern, "yyyy-MM-dd", 22, 0, 0),
			new(TokenType.BraceClose, "}", 32, 0, 0),
			new(TokenType.EOF, "", 33, 0, 0)
		];
		List<Token> actualTokens = [];

		// Act
		while (lexer.HasNextToken())
		{
			actualTokens.Add(lexer.NextToken());
		}

		actualTokens.Add(lexer.NextToken());

		// Assert
		AssertTokens(expectedTokens, actualTokens);
	}

	[Fact]
	public void ScanToken_EvalPattern_ReturnsCorrectTokens()
	{
		// Arrange
		Lexer2 lexer = new("${count§if,eq 0?0:`low`}");
		List<Token> expectedTokens =
		[
			new(TokenType.DollarBraceOpen, "${", 0, 0, 0),
			new(TokenType.Identifier, "count", 2, 0, 0),
			new(TokenType.Section, "§", 7, 0, 0),
			new(TokenType.Identifier, "if", 8, 0, 0),
			new(TokenType.Comma, ",", 10, 0, 0),
			new(TokenType.Identifier, "eq", 11, 0, 0),
			new(TokenType.LiteralInteger, "0", 14, 0, 0),
			new(TokenType.QuestionMark, "?", 15, 0, 0),
			new(TokenType.LiteralInteger, "0", 16, 0, 0),
			new(TokenType.Colon, ":", 17, 0, 0),
			new(TokenType.LiteralString, "low", 18, 0, 0),
			new(TokenType.BraceClose, "}", 23, 0, 0),
			new(TokenType.EOF, "", 24, 0, 0)
		];
		List<Token> actualTokens = new();

		// Act
		while (lexer.HasNextToken())
		{
			actualTokens.Add(lexer.NextToken());
		}

		actualTokens.Add(lexer.NextToken());

		// Assert
		AssertTokens(expectedTokens, actualTokens);
	}

	[Fact]
	public void ScanToken_EscapedBraces_ReturnsCorrectTokens()
	{
		// Arrange
		Lexer2 lexer = new("text {{escaped}} text");
		List<Token> expectedTokens =
		[
			new(TokenType.LiteralString, "text {escaped} text", 0, 0, 0),
			new(TokenType.EOF, "", 21, 0, 0)
		];
		List<Token> actualTokens = [];

		// Act
		while (lexer.HasNextToken())
		{
			actualTokens.Add(lexer.NextToken());
		}

		actualTokens.Add(lexer.NextToken());

		// Assert
		AssertTokens(expectedTokens, actualTokens);
	}

	[Fact]
	public void ScanToken_PlainText_ReturnsSimpleSingleLiteralString()
	{
		// Arrange
		Lexer2 lexer = new("hello");
		List<Token> expectedTokens =
		[
			new(TokenType.LiteralString, "hello", 0, 0, 0),
			new(TokenType.EOF, "", 5, 0, 0)
		];
		List<Token> actualTokens = [];

		// Act
		while (lexer.HasNextToken())
		{
			actualTokens.Add(lexer.NextToken());
		}

		actualTokens.Add(lexer.NextToken());

		// Assert
		AssertTokens(expectedTokens, actualTokens);
	}

	[Fact]
	public void ScanToken_PlainText_ReturnsSingleLiteralString()
	{
		// Arrange
		Lexer2 lexer = new("hello world");
		List<Token> expectedTokens =
		[
			new(TokenType.LiteralString, "hello world", 0, 0, 0),
			new(TokenType.EOF, "", 11, 0, 0)
		];
		List<Token> actualTokens = [];

		// Act
		while (lexer.HasNextToken())
		{
			actualTokens.Add(lexer.NextToken());
		}

		actualTokens.Add(lexer.NextToken());

		// Assert
		AssertTokens(expectedTokens, actualTokens);
	}

	[Fact]
	public void ScanToken_LoneDollarOrHash_ReturnsLiteralString()
	{
		// Arrange
		Lexer2 lexer = new("text $ #");
		List<Token> expectedTokens =
		[
			new(TokenType.LiteralString, "text $ #", 0, 0, 0),
			new(TokenType.EOF, "", 8, 0, 0)
		];
		List<Token> actualTokens = [];

		// Act
		while (lexer.HasNextToken())
		{
			actualTokens.Add(lexer.NextToken());
		}

		actualTokens.Add(lexer.NextToken());

		// Assert
		AssertTokens(expectedTokens, actualTokens);
	}

	[Fact]
	public void ScanToken_EmptyInput_ReturnsEOF()
	{
		// Arrange
		Lexer2 lexer = new("");
		List<Token> expectedTokens =
		[
			new(TokenType.EOF, "", 0, 0, 0)
		];
		List<Token> actualTokens = [];

		// Act
		while (lexer.HasNextToken())
		{
			actualTokens.Add(lexer.NextToken());
		}

		actualTokens.Add(lexer.NextToken());

		// Assert
		AssertTokens(expectedTokens, actualTokens);
	}

	[Fact]
	public void ScanToken_UnterminatedPlaceholder_ThrowsException()
	{
		// Arrange
		Lexer2 lexer = new("${name");

		// Act & Assert
		Assert.Throws<InvalidOperationException>(() =>
		{
			while (lexer.HasNextToken())
			{
				lexer.NextToken();
			}
		});
	}

	[Fact]
	public void HasMoreTokens_ReturnsFalseAfterEOF()
	{
		// Arrange
		Lexer2 lexer = new("text");

		// Act
		while (lexer.HasNextToken())
		{
			lexer.NextToken();
		}

		// Assert
		Assert.False(lexer.HasNextToken());
	}
}