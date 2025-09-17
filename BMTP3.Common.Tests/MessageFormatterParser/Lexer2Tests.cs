using BMTP3.Common.MessageFormatterParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Common.Tests.MessageFormatterParser;

public class Lexer2Tests {
	private void AssertTokensx(List<Token> expected, List<Token> actual) {
		Assert.Equal(expected.Count, actual.Count);
		for(int i = 0; i < expected.Count; i++) {
			Assert.Equal(expected[i].Type, actual[i].Type);
			Assert.Equal(expected[i].Value, actual[i].Value);
			Assert.Equal(expected[i].Position, actual[i].Position);
		}
	}
	private void AssertTokens(List<Token> expected, List<Token> actual) {
		Assert.True(expected.Count == actual.Count, $"Expected {expected.Count} tokens but found {actual.Count}.");

		for(int i = 0; i < expected.Count; i++) {
			var expectedToken = expected[i];
			var actualToken = actual[i];

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
	public void ScanToken_SimpleInput_ReturnsCorrectTokens() {
		// Arrange
		Lexer2 lexer = new("some text $x ${ name }");
		List<Token> expectedTokens = [
			new Token(TokenType.LiteralString, "some text $x ", 0),
			new Token(TokenType.DollarBraceOpen, "${", 13),
			new Token(TokenType.Identifier, "name", 16),
			new Token(TokenType.BraceClose, "}", 21),
			new Token(TokenType.EOF, "", 22)
		];
		List<Token> actualTokens = [];

		// Act
		while(lexer.HasNextToken()) {
			actualTokens.Add(lexer.NextToken());
		}
		actualTokens.Add(lexer.NextToken());

		// Assert
		AssertTokens(expectedTokens, actualTokens);
	}

	[Fact]
	public void ScanToken_ComplexInput_ReturnsCorrectTokens() {
		// Arrange
		Lexer2 lexer = new("${date.toUpper(),date:yyyy-MM-dd}");
		List<Token> expectedTokens = [
			new Token(TokenType.DollarBraceOpen, "${", 0),
			new Token(TokenType.Identifier, "date", 2),
			new Token(TokenType.Dot, ".", 6),
			new Token(TokenType.Identifier, "toUpper", 7),
			new Token(TokenType.ParenOpen, "(", 14),
			new Token(TokenType.ParenClose, ")", 15),
			new Token(TokenType.Comma, ",", 16),
			new Token(TokenType.Identifier, "date", 17),
			new Token(TokenType.Colon, ":", 21),
			new Token(TokenType.LiteralPattern, "yyyy-MM-dd", 22),
			new Token(TokenType.BraceClose, "}", 32),
			new Token(TokenType.EOF, "", 33)
		];
		List<Token> actualTokens = [];

		// Act
		while(lexer.HasNextToken()) {
			actualTokens.Add(lexer.NextToken());
		}
		actualTokens.Add(lexer.NextToken());

		// Assert
		AssertTokens(expectedTokens, actualTokens);
	}

	[Fact]
	public void ScanToken_EvalPattern_ReturnsCorrectTokens() {
		// Arrange
		Lexer2 lexer = new("${count§if,eq 0?0:`low`}");
		List<Token> expectedTokens = [
			new Token(TokenType.DollarBraceOpen, "${", 0),
			new Token(TokenType.Identifier, "count", 2),
			new Token(TokenType.Section, "§", 7),
			new Token(TokenType.Identifier, "if", 8),
			new Token(TokenType.Comma, ",", 10),
			new Token(TokenType.Identifier, "eq", 11),
			new Token(TokenType.LiteralInteger, "0", 14),
			new Token(TokenType.QuestionMark, "?", 15),
			new Token(TokenType.LiteralInteger, "0", 16),
			new Token(TokenType.Colon, ":", 17),
			new Token(TokenType.LiteralString, "low", 18),
			new Token(TokenType.BraceClose, "}", 23),
			new Token(TokenType.EOF, "", 24)
		];
		List<Token> actualTokens = new();

		// Act
		while(lexer.HasNextToken()) {
			actualTokens.Add(lexer.NextToken());
		}
		actualTokens.Add(lexer.NextToken());

		// Assert
		AssertTokens(expectedTokens, actualTokens);
	}

	[Fact]
	public void ScanToken_EscapedBraces_ReturnsCorrectTokens() {
		// Arrange
		Lexer2 lexer = new("text {{escaped}} text");
		List<Token> expectedTokens = [
			new Token(TokenType.LiteralString, "text {escaped} text", 0),
			new Token(TokenType.EOF, "", 21)
		];
		List<Token> actualTokens = [];

		// Act
		while(lexer.HasNextToken()) {
			actualTokens.Add(lexer.NextToken());
		}
		actualTokens.Add(lexer.NextToken());

		// Assert
		AssertTokens(expectedTokens, actualTokens);
	}

	[Fact]
	public void ScanToken_PlainText_ReturnsSimpleSingleLiteralString() {
		// Arrange
		Lexer2 lexer = new("hello");
		List<Token> expectedTokens = [
			new Token(TokenType.LiteralString, "hello", 0),
			new Token(TokenType.EOF, "", 5)
		];
		List<Token> actualTokens = [];

		// Act
		while(lexer.HasNextToken()) {
			actualTokens.Add(lexer.NextToken());
		}
		actualTokens.Add(lexer.NextToken());

		// Assert
		AssertTokens(expectedTokens, actualTokens);
	}

	[Fact]
	public void ScanToken_PlainText_ReturnsSingleLiteralString() {
		// Arrange
		Lexer2 lexer = new("hello world");
		List<Token> expectedTokens = [
			new Token(TokenType.LiteralString, "hello world", 0),
			new Token(TokenType.EOF, "", 11)
		];
		List<Token> actualTokens = [];

		// Act
		while(lexer.HasNextToken()) {
			actualTokens.Add(lexer.NextToken());
		}
		actualTokens.Add(lexer.NextToken());

		// Assert
		AssertTokens(expectedTokens, actualTokens);
	}

	//[Fact]
	public void ScanToken_LoneDollarOrHash_ReturnsLiteralString() {
		// Arrange
		Lexer2 lexer = new("text $ #");
		List<Token> expectedTokens = [
			new Token(TokenType.LiteralString, "text $ #", 0),
			new Token(TokenType.EOF, "", 8)
		];
		List<Token> actualTokens = [];

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
		Lexer2 lexer = new("");
		List<Token> expectedTokens = [
			new Token(TokenType.EOF, "", 0)
		];
		List<Token> actualTokens = [];

		// Act
		while(lexer.HasNextToken()) {
			actualTokens.Add(lexer.NextToken());
		}
		actualTokens.Add(lexer.NextToken());

		// Assert
		AssertTokens(expectedTokens, actualTokens);
	}

	[Fact]
	public void ScanToken_UnterminatedPlaceholder_ThrowsException() {
		// Arrange
		Lexer2 lexer = new("${name");

		// Act & Assert
		Assert.Throws<InvalidOperationException>(() => {
			while(lexer.HasNextToken()) {
				lexer.NextToken();
			}
		});
	}

	[Fact]
	public void HasMoreTokens_ReturnsFalseAfterEOF() {
		// Arrange
		Lexer2 lexer = new("text");

		// Act
		while(lexer.HasNextToken()) {
			lexer.NextToken();
		}

		// Assert
		Assert.False(lexer.HasNextToken());
	}
}