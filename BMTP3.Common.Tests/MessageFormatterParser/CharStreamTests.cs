using BMTP3.Common.MessageFormatterParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Common.Tests.MessageFormatterParser {
	public class CharStreamTests {
		[Fact]
		public void Next_WithSingleChar_ReturnsCorrectCharAndUpdatesPosition() {
			// Arrange
			var input = "a";
			var stream = new CharStream(input);

			// Act
			char result = stream.Next();

			bool hasMoreChars = stream.HasChars(1);

			// Assert
			Assert.False(hasMoreChars);
			Assert.Equal('a', result);
			Assert.Equal(1, stream.LineNumber);
			Assert.Equal(2, stream.ColumnNumber);
			Assert.True(stream.EndOfStream);
		}

		[Fact]
		public void Next_WithEmptyStream_ThrowsException() {
			// Arrange
			var stream = new CharStream("");

			// Act & Assert
			int lineNumberExpected = 1;
			int columnNumberExpected = 1;

			var exception = Assert.Throws<CharStreamException>(() => stream.Next());
			Assert.Equal($"End of stream at line {lineNumberExpected}, column {columnNumberExpected}", exception.Message);
			Assert.Equal(lineNumberExpected, exception.LineNumber);
			Assert.Equal(columnNumberExpected, exception.ColumnNumber);
		}

		[Fact]
		public void NextMultiple_WithMultipleChars_ReturnsCorrectListAndUpdatesPosition() {
			// Arrange
			var input = "hello";
			var stream = new CharStream(input);

			// Act
			List<char> result = stream.Next(3);

			bool hasTwoMoreChars = stream.HasChars(2);
			bool hasThreeMoreChars = stream.HasChars(3);

			// Assert
			Assert.True(hasTwoMoreChars);
			Assert.False(hasThreeMoreChars);
			Assert.Equal(['h', 'e', 'l'], result);
			Assert.Equal(1, stream.LineNumber);
			Assert.Equal(4, stream.ColumnNumber);
			Assert.False(stream.EndOfStream);
		}

		[Fact]
		public void NextMultiple_WithTooManyChars_ThrowsException() {
			// Arrange
			var input = "ab";
			var stream = new CharStream(input);

			// Act & Assert
			int lineNumberExpected = 1;
			int columnNumberExpected = 3;

			var exception = Assert.Throws<CharStreamException>(() => stream.Next(3));
			Assert.Equal($"Not enough characters in stream (requested 3, missing 1) at line {lineNumberExpected}, column {columnNumberExpected}", exception.Message);
			Assert.Equal(lineNumberExpected, exception.LineNumber);
			Assert.Equal(columnNumberExpected, exception.ColumnNumber);
		}

		[Fact]
		public void Peek_WithSingleChar_ReturnsCharWithoutConsuming() {
			// Arrange
			var input = "a";
			var stream = new CharStream(input);

			// Act
			char result1 = stream.Peek();
			char result2 = stream.Next();

			// Assert
			Assert.Equal('a', result1);
			Assert.Equal('a', result2);
			Assert.Equal(1, stream.LineNumber);
			Assert.Equal(2, stream.ColumnNumber);
			Assert.True(stream.EndOfStream);
		}

		[Fact]
		public void PeekMultiple_WithMultipleChars_ReturnsCorrectListWithoutConsuming() {
			// Arrange
			var input = "hello";
			var stream = new CharStream(input);

			// Act
			List<char> result1 = stream.Peek(3);
			List<char> result2 = stream.Next(3);

			// Assert
			Assert.Equal(['h', 'e', 'l'], result1);
			Assert.Equal(['h', 'e', 'l'], result2);
			Assert.Equal(1, stream.LineNumber);
			Assert.Equal(4, stream.ColumnNumber);
			Assert.False(stream.EndOfStream);
		}

		[Fact]
		public void PeekMultiple_WithTooManyChars_ThrowsException() {
			// Arrange
			var input = "ab";
			var stream = new CharStream(input);

			// Act & Assert
			int lineNumberExpected = 1;
			int columnNumberExpected = 1;

			var exception = Assert.Throws<CharStreamException>(() => stream.Peek(3));
			Assert.Equal($"Not enough characters in stream (requested 3, found 2) at line {lineNumberExpected}, column {columnNumberExpected}", exception.Message);
			Assert.Equal(lineNumberExpected, exception.LineNumber);
			Assert.Equal(columnNumberExpected, exception.ColumnNumber);
		}

		[Fact]
		public void HasChars_WithEnoughChars_ReturnsTrue() {
			// Arrange
			var input = "hello";
			var stream = new CharStream(input);

			// Act
			bool result = stream.HasChars(3);

			// Assert
			Assert.True(result);
			Assert.Equal(1, stream.LineNumber);
			Assert.Equal(1, stream.ColumnNumber); // Position uændret
		}

		[Fact]
		public void HasChars_WithTooManyChars_ReturnsFalse() {
			// Arrange
			var input = "ab";
			var stream = new CharStream(input);

			// Act
			bool result = stream.HasChars(3);

			// Assert
			Assert.False(result);
			Assert.Equal(1, stream.LineNumber);
			Assert.Equal(1, stream.ColumnNumber); // Position uændret
		}

		[Fact]
		public void EndOfStream_WithEmptyStream_ReturnsTrue() {
			// Arrange
			var input = "";
			var stream = new CharStream(input);

			// Assert
			Assert.True(stream.EndOfStream);
		}

		[Fact]
		public void EndOfStream_AfterReadingAllChars_ReturnsTrue() {
			// Arrange
			var input = "ab";
			var stream = new CharStream(input);

			// Act
			stream.Next(2);

			// Assert
			Assert.True(stream.EndOfStream);
		}

		[Fact]
		public void LineAndColumnTracking_WithNewLine_UpdatesCorrectly() {
			// Arrange
			var input = "a\nb";
			var stream = new CharStream(input);

			// Act
			stream.Next(); // 'a'
			stream.Next(); // '\n'
			stream.Next(); // 'b'

			// Assert
			Assert.Equal(2, stream.LineNumber);
			Assert.Equal(2, stream.ColumnNumber);
			Assert.True(stream.EndOfStream);
		}

		[Theory]
		[InlineData("hello", 5, 1, 6)]
		[InlineData("a\nb", 3, 2, 2)]
		[InlineData("", 0, 1, 1)]
		public void PositionTracking_MultipleReads_UpdatesCorrectly(string input, int charCount, int expectedLine, int expectedColumn) {
			// Arrange
			var stream = new CharStream(input);

			// Act
			if(charCount > 0)
				stream.Next(charCount);

			// Assert
			Assert.Equal(expectedLine, stream.LineNumber);
			Assert.Equal(expectedColumn, stream.ColumnNumber);
		}
	}
}
