using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Common.MessageFormatterParser {
	public class CharStream {
		private readonly Stream stream;
		private readonly StreamReader reader;
		private readonly Queue<char> peekBuffer = new Queue<char>(); // Dynamisk buffer
		private int lineNumber = 1;
		private int columnNumber = 1;

		/// <summary>
		/// Initializes a new instance of the CharStream class with a string input.
		/// </summary>
		/// <param name="input">The string to be used as the stream source.</param>
		public CharStream(string input) : this(new MemoryStream(Encoding.UTF8.GetBytes(input))) { }

		/// <summary>
		/// Initializes a new instance of the CharStream class with a Stream object.
		/// </summary>
		/// <param name="stream">The stream to be read from.</param>
		/// <exception cref="ArgumentNullException">Thrown if the stream is null.</exception>
		public CharStream(Stream stream) {
			this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
			this.reader = new StreamReader(stream);
		}

		/// <summary>
		/// Retrieves the next character from the stream and advances the position.
		/// Throws a CharStreamException if the end of the stream is reached.
		/// </summary>
		/// <returns>The next character in the stream.</returns>
		/// <exception cref="CharStreamException">Thrown if the end of the stream is encountered.</exception>
		public char Next() {
			if(peekBuffer.Count > 0) {
				char c = peekBuffer.Dequeue();
				UpdatePosition(c);
				return c;
			}
			if(reader.EndOfStream) {
				throw new CharStreamException($"End of stream at line {lineNumber}, column {columnNumber}", lineNumber, columnNumber);
			}
			char next = (char)reader.Read();
			UpdatePosition(next);
			return next;
		}

		/// <summary>
		/// Retrieves a specified number of characters from the stream as a list and advances the position.
		/// </summary>
		/// <param name="count">The number of characters to retrieve.</param>
		/// <returns>A list containing the retrieved characters.</returns>
		/// <exception cref="ArgumentException">Thrown if the character count is negative.</exception>
		/// <exception cref="CharStreamException">Thrown if there are not enough characters in the stream to satisfy the request.</exception>
		public List<char> Next(int count) {
			if(count < 0) throw new ArgumentException("Character count cannot be negative");
			var result = new List<char>();
			for(int i = 0; i < count; i++) {
				if(peekBuffer.Count > 0) {
					result.Add(peekBuffer.Dequeue());
				} else if(!reader.EndOfStream) {
					result.Add((char)reader.Read());
				} else {
					throw new CharStreamException($"Not enough characters in stream (requested {count}, missing {count - i}) at line {lineNumber}, column {columnNumber}", lineNumber, columnNumber);
				}
				UpdatePosition(result[i]);
			}
			return result;
		}

		/// <summary>
		/// Peeks at the next character in the stream without consuming it.
		/// </summary>
		/// <returns>The next character in the stream.</returns>
		/// <exception cref="CharStreamException">Thrown if the end of the stream is encountered.</exception>
		public char Peek() {
			if(peekBuffer.Count > 0) {
				return peekBuffer.Peek();
			}
			if(reader.EndOfStream) {
				throw new CharStreamException($"End of stream at line {lineNumber}, column {columnNumber}", lineNumber, columnNumber);
			}
			char next = (char)reader.Read();
			peekBuffer.Enqueue(next);
			return next;
		}

		/// <summary>
		/// Peeks at a specified number of characters in the stream as a list without consuming them.
		/// </summary>
		/// <param name="count">The number of characters to peek at.</param>
		/// <returns>A list containing the peeked characters.</returns>
		/// <exception cref="ArgumentException">Thrown if the character count is negative.</exception>
		/// <exception cref="CharStreamException">Thrown if there are not enough characters in the stream to satisfy the request.</exception>
		public List<char> Peek(int count) {
			if(count < 0) throw new ArgumentException("Character count cannot be negative");
			var result = new List<char>();
			while(peekBuffer.Count < count && !reader.EndOfStream) {
				peekBuffer.Enqueue((char)reader.Read());
			}
			if(peekBuffer.Count < count) {
				throw new CharStreamException($"Not enough characters in stream (requested {count}, found {peekBuffer.Count}) at line {lineNumber}, column {columnNumber}", lineNumber, columnNumber);
			}
			return peekBuffer.Take(count).ToList();
		}

		/// <summary>
		/// Checks if there are at least a specified number of characters remaining in the stream.
		/// This method does not consume any characters.
		/// </summary>
		/// <param name="count">The minimum number of characters to check for.</param>
		/// <returns>True if there are at least 'count' characters available; otherwise, false.</returns>
		public bool HasChars(int count) {
			if(count < 0) return false;
			while(peekBuffer.Count < count && !reader.EndOfStream) {
				peekBuffer.Enqueue((char)reader.Read());
			}
			return peekBuffer.Count >= count;
		}

		private void UpdatePosition(char c) {
			columnNumber++;
			if(c == '\n') { lineNumber++; columnNumber = 1; }
		}

		/// <summary>
		/// Gets the current line number in the stream.
		/// </summary>
		public int LineNumber => lineNumber;

		/// <summary>
		/// Gets the current column number in the stream.
		/// </summary>
		public int ColumnNumber => columnNumber;

		/// <summary>
		/// Gets a value indicating whether the end of the stream has been reached.
		/// </summary>
		public bool EndOfStream => peekBuffer.Count == 0 && reader.EndOfStream;
	}
}
