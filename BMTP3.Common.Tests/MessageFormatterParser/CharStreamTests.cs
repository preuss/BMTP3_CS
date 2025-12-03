using BMTP3.Common.MessageFormatterParser;

namespace BMTP3.Common.Tests.MessageFormatterParser
{
	public class CharStreamTests
	{
		[Fact]
		public void Next_WithSingleChar_ReturnsCorrectCharAndUpdatesPosition()
		{
			// Arrange
			var input = "a";
			var stream = new CharStream(input);

			// Act
			char result = stream.Next();

			bool hasMoreChars = stream.HasChars(1);

			// Assert
			Assert.False(hasMoreChars);
			Assert.Equal('a', result);
			Assert.Equal(0, stream.LineNumber);
			Assert.Equal(1, stream.ColumnNumber);
			Assert.True(stream.EndOfStream);
		}

		[Fact]
		public void Next_WithEmptyStream_ThrowsException()
		{
			// Arrange
			var stream = new CharStream("");

			// Act & Assert
			int lineNumberExpected = 0;
			int columnNumberExpected = 0;

			var exception = Assert.Throws<CharStreamException>(() => stream.Next());
			Assert.Equal($"End of stream at line {lineNumberExpected}, column {columnNumberExpected}", exception.Message);
			Assert.Equal(lineNumberExpected, exception.LineNumber);
			Assert.Equal(columnNumberExpected, exception.ColumnNumber);
		}

		[Fact]
		public void NextMultiple_WithMultipleChars_ReturnsCorrectListAndUpdatesPosition()
		{
			// Arrange
			var input = "hello\r\nworld"; // Default new line is \n, but this stream is initialized with \r\n, so we'll pass it explicitly.
										  // The CharStream constructor without new line sequences defaults to POSIX (\n).
										  // We need to ensure the stream correctly identifies the Windows_DOS newline.
			var stream = new CharStream(input, new[] { CharStream.WINDOWS_DOS });


			// Act
			List<char> result1 = stream.Next(3);
			Assert.Equal(['h', 'e', 'l'], result1);
			Assert.Equal(0, stream.LineNumber);
			Assert.Equal(3, stream.ColumnNumber);
			Assert.False(stream.EndOfStream);

			List<char> result2 = stream.Next(3);

			// Assert
			Assert.Equal(['l', 'o', '\n'], result2);
			Assert.Equal(1, stream.LineNumber);
			Assert.Equal(0, stream.ColumnNumber);

			List<char> result3 = stream.Next(5);
			Assert.Equal(['w', 'o', 'r', 'l', 'd'], result3);
			Assert.Equal(1, stream.LineNumber);
			Assert.Equal(5, stream.ColumnNumber);
			Assert.True(stream.EndOfStream);
		}

		[Fact]
		public void NextMultiple_WithTooManyChars_ThrowsException()
		{
			// Arrange
			var input = "a\nb";
			var stream = new CharStream(input);

			// Act & Assert
			int lineNumberExpected = 1;
			int columnNumberExpected = 1;

			var exception = Assert.Throws<CharStreamException>(() => stream.Next(4));

			Assert.Equal($"End of stream at line {lineNumberExpected}, column {columnNumberExpected}", exception.Message);
			Assert.Equal(lineNumberExpected, exception.LineNumber);
			Assert.Equal(columnNumberExpected, exception.ColumnNumber);
		}

		[Fact]
		public void Peek_WithSingleChar_ReturnsCharWithoutConsuming()
		{
			// Arrange
			var input = "a";
			var stream = new CharStream(input);

			// Act
			char result1 = stream.Peek();
			char result2 = stream.Next();

			// Assert
			Assert.Equal('a', result1);
			Assert.Equal('a', result2);
			Assert.Equal(0, stream.LineNumber);
			Assert.Equal(1, stream.ColumnNumber);
			Assert.True(stream.EndOfStream);
		}

		[Fact]
		public void PeekMultiple_WithMultipleChars_ReturnsCorrectListWithoutConsuming()
		{
			// Arrange
			var input = "hello";
			var stream = new CharStream(input);

			// Act
			List<char> result1 = stream.Peek(3);
			List<char> result2 = stream.Next(3);

			// Assert
			Assert.Equal(['h', 'e', 'l'], result1);
			Assert.Equal(['h', 'e', 'l'], result2);
			Assert.Equal(0, stream.LineNumber);
			Assert.Equal(3, stream.ColumnNumber);
			Assert.False(stream.EndOfStream);
		}

		[Fact]
		public void PeekMultiple_WithTooManyChars_ThrowsException()
		{
			// Arrange
			var input = "ab";
			var stream = new CharStream(input);

			// Act & Assert
			int lineNumberExpected = 0;
			int columnNumberExpected = 0;

			var exception = Assert.Throws<CharStreamException>(() => stream.Peek(3));
			Assert.Equal($"Not enough characters in stream (requested 3, found 2) at line {lineNumberExpected}, column {columnNumberExpected}", exception.Message);
			Assert.Equal(lineNumberExpected, exception.LineNumber);
			Assert.Equal(columnNumberExpected, exception.ColumnNumber);
		}

		[Fact]
		public void HasChars_WithEnoughChars_ReturnsTrue()
		{
			// Arrange
			var input = "hello";
			var stream = new CharStream(input);

			// Act
			bool result = stream.HasChars(3);

			// Assert
			Assert.True(result);
			Assert.Equal(0, stream.LineNumber);
			Assert.Equal(0, stream.ColumnNumber); // Position unchanged
		}

		[Fact]
		public void HasChars_WithTooManyChars_ReturnsFalse()
		{
			// Arrange
			var input = "ab";
			var stream = new CharStream(input);

			// Act
			bool result = stream.HasChars(3);

			// Assert
			Assert.False(result);
			Assert.Equal(0, stream.LineNumber);
			Assert.Equal(0, stream.ColumnNumber); // Position unchanged
		}

		[Fact]
		public void EndOfStream_WithEmptyStream_ReturnsTrue()
		{
			// Arrange
			var input = "";
			var stream = new CharStream(input);

			// Assert
			Assert.True(stream.EndOfStream);
		}

		[Fact]
		public void EndOfStream_AfterReadingAllChars_ReturnsTrue()
		{
			// Arrange
			var input = "ab";
			var stream = new CharStream(input);

			// Act
			stream.Next(2);

			// Assert
			Assert.True(stream.EndOfStream);
		}

		[Theory]
		[InlineData("a\nb", CharStream.POSIX)]         // POSIX
		[InlineData("a\r\nb", CharStream.WINDOWS_DOS)] // Windows/DOS
		[InlineData("a\rb", CharStream.COMMODORE)]    // Commodore
		[InlineData("a\n\rb", CharStream.ACORN)]      // Acorn
		public void LineAndColumnTracking_WithVariousNewLines_UpdatesCorrectly(string input, string newLineSequence)
		{
			// Arrange
			var stream = new CharStream(input, [newLineSequence]);

			var expectedLineAfterNewLine = 1;
			var expectedColumnAfterNewLine = 0;
			var expectedColumnAfterLastChar = 1;

			// Act & Assert for char 1
			char actualChar1 = stream.Next();
			char expectedFirstChar = 'a';
			Assert.Equal(expectedFirstChar, actualChar1);
			Assert.Equal(0, stream.LineNumber);
			Assert.Equal(1, stream.ColumnNumber);

			// Act & Assert for char 2 (newline)
			char actualChar2 = stream.Next();
			char expectedNewLineChar = '\n';
			Assert.Equal(expectedNewLineChar, actualChar2);
			Assert.Equal(expectedLineAfterNewLine, stream.LineNumber);
			Assert.Equal(expectedColumnAfterNewLine, stream.ColumnNumber);

			// Act & Assert for char 3
			char actualChar3 = stream.Next();
			char expectedLastChar = 'b';
			Assert.Equal(expectedLastChar, actualChar3);
			Assert.Equal(expectedLineAfterNewLine, stream.LineNumber);
			Assert.Equal(expectedColumnAfterLastChar, stream.ColumnNumber);
			Assert.True(stream.EndOfStream);
		}

		[Fact]
		public void Next_WithZeroCount_ReturnsEmptyListAndNoPositionChange()
		{
			// Arrange
			var input = "abc";
			var stream = new CharStream(input);

			// Act
			var result = stream.Next(0);

			// Assert
			Assert.Empty(result);
			Assert.Equal(0, stream.LineNumber);
			Assert.Equal(0, stream.ColumnNumber);
			Assert.False(stream.EndOfStream); // Stream is not consumed
		}

		[Fact]
		public void Peek_WithZeroCount_ReturnsEmptyListAndNoPositionChange()
		{
			// Arrange
			var input = "abc";
			var stream = new CharStream(input);

			// Act
			var result = stream.Peek(0);

			// Assert
			Assert.Empty(result);
			Assert.Equal(0, stream.LineNumber);
			Assert.Equal(0, stream.ColumnNumber);
			Assert.False(stream.EndOfStream); // Stream is not consumed
		}

		[Theory]
		[InlineData("hello", 5, 0, 5)]
		[InlineData("a\nb", 3, 1, 1)]
		[InlineData("a\r\nb", 3, 1, 1)]
		[InlineData("a\rb", 3, 1, 1)]
		[InlineData("a\n\rb", 3, 1, 1)]
		[InlineData("", 0, 0, 0)]
		public void PositionTracking_MultipleReads_UpdatesCorrectly(string input, int charCount, int expectedLine, int expectedColumn)
		{
			// Arrange
			var allNewLineSequences = new[] { CharStream.POSIX, CharStream.WINDOWS_DOS, CharStream.COMMODORE, CharStream.ACORN };
			var stream = new CharStream(input, allNewLineSequences);

			// Act
			if(charCount > 0)
			{
				stream.Next(charCount);
			}

			// Assert
			Assert.Equal(expectedLine, stream.LineNumber);
			Assert.Equal(expectedColumn, stream.ColumnNumber);
		}
	}
}
