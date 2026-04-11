using System.Text;

namespace BMTP3.Common.MessageFormatterParser;

public class CharStream
{
	public const string POSIX = "\n"; // LF    = Unix-style line endings
	public const string WINDOWS_DOS = "\r\n"; // CR LF = Windows / DOS-style line endings
	public const string COMMODORE = "\r"; // CR    = Commodore-style line endings
	public const string ACORN = "\n\r"; // LF CR = Acorn-style line endings

	private readonly char defaultNewLineChar = '\n';
	private readonly string[] newLineSequences;
	private readonly Queue<char> peekBuffer = new(); // Dynamisk buffer
	private readonly StreamReader reader;

	private readonly Stream stream;

	/// <summary>
	///     Initializes a new instance of the CharStream class with a string input.
	/// </summary>
	/// <param name="input">The string to be used as the stream source.</param>
	public CharStream(string input) : this(new MemoryStream(Encoding.UTF8.GetBytes(input)))
	{
	}

	/// <summary>
	///     Initializes a new instance of the CharStream class with a Stream object.
	/// </summary>
	/// <param name="stream">The stream to be read from.</param>
	/// <exception cref="ArgumentNullException">Thrown if the stream is null.</exception>
	public CharStream(Stream stream) : this(stream, [POSIX])
	{
	}

	public CharStream(string input, string[] newLineSequences) : this(new MemoryStream(Encoding.UTF8.GetBytes(input)),
		newLineSequences)
	{
	}

	/// <summary>
	///     Initializes a new instance of the CharStream class with a Stream object and an array of new line sequences.
	/// </summary>
	/// <param name="stream"></param>
	/// <param name="newLineSequences"></param>
	/// <exception cref="ArgumentNullException">Thrown if the stream is null.</exception>
	/// <exception cref="ArgumentException">Thrown if the new line sequences array is null or empty.</exception>
	/// <exception cref="ArgumentException">Thrown if any of the new line sequences are not a valid new line sequence.</exception>
	public CharStream(Stream stream, string[] newLineSequences)
	{
		this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
		reader = new StreamReader(stream);

		if (newLineSequences == null || newLineSequences.Length == 0)
		{
			throw new ArgumentException("New line sequences cannot be null or empty", nameof(newLineSequences));
		}

		HashSet<string> validNewLineSequencesSet = new() { POSIX, WINDOWS_DOS, COMMODORE, ACORN };
		foreach (string newLineSequence in newLineSequences)
		{
			if (!validNewLineSequencesSet.Contains(newLineSequence))
			{
				throw new ArgumentException($"Invalid new line sequence: {newLineSequence}",
					nameof(newLineSequences));
			}
		}

		string[] validOrderOfNewLineSequences = newLineSequences.OrderByDescending(s => s.Length).ToArray();
		this.newLineSequences = validOrderOfNewLineSequences;
	}

	/// <summary>
	///     Gets the current line number in the stream.
	/// </summary>
	public int LineNumber { get; private set; }

	/// <summary>
	///     Gets the current column number in the stream.
	/// </summary>
	public int ColumnNumber { get; private set; }

	/// <summary>
	///     Gets a value indicating whether the end of the stream has been reached.
	/// </summary>
	public bool EndOfStream => peekBuffer.Count == 0 && reader.EndOfStream;

	/// <summary>
	///     Attempts to identify and consume a newline sequence at the current stream position.
	///     If a sequence is found, line and column numbers are updated, and the sequence characters are removed from the
	///     stream.
	/// </summary>
	/// <returns>The length of the identified and consumed newline sequence, or 0 if no sequence was found.</returns>
	private int TryAdvanceNewLine()
	{
		int localMinNewLineLength = newLineSequences.Min(s => s.Length);
		int localMaxNewLineLength = newLineSequences.Max(s => s.Length);

		if (!HasChars(1))
		{
			return 0;
		}

		int peekCount = HasCharsCount(localMaxNewLineLength);

		if (peekCount == 0)
		{
			return 0;
		}

		if (peekCount < localMinNewLineLength)
		{
			return 0; // Not enough characters available to match any newline sequence.
		}

		List<char> peekedChars = Peek(peekCount);

		// Iterate through newline sequences, prioritized by length (longest first).
		foreach (string newLineSequence in newLineSequences)
		{
			if (peekedChars.Count >= newLineSequence.Length)
			{
				bool matches = true;
				for (int i = 0; i < newLineSequence.Length; i++)
				{
					if (peekedChars[i] != newLineSequence[i])
					{
						matches = false;
						break; // Mismatch. Check next sequence.
					}
				}

				if (matches)
				{
					LineNumber++;
					ColumnNumber = 0; // Reset column for the new line.

					// Consume characters of the matched newline sequence.
					for (int i = 0; i < newLineSequence.Length; i++)
					{
						if (peekBuffer.Count > 0)
						{
							peekBuffer.Dequeue(); // From internal buffer.
						}
						else
						{
							reader.Read(); // Directly from underlying stream.
						}
					}

					return newLineSequence.Length; // Return length of consumed sequence.
				}
			}
		}

		return 0; // No matching newline sequence found.
	}


	/// <summary>
	///     Retrieves the next character from the stream and advances the position.
	///     Throws a CharStreamException if the end of the stream is reached.
	/// </summary>
	/// <returns>The next character in the stream.</returns>
	/// <exception cref="CharStreamException">Thrown if the end of the stream is encountered.</exception>
	public char Next()
	{
		int consumedNewLineLength = TryAdvanceNewLine();

		if (consumedNewLineLength > 0)
		{
			// Line and column numbers are already updated by TryAdvanceNewLine().
			return defaultNewLineChar;
		}

		// If no newline was consumed, check if there are any characters left.
		if (EndOfStream)
		{
			throw new CharStreamException($"End of stream at line {LineNumber}, column {ColumnNumber}", LineNumber,
				ColumnNumber);
		}

		char c;
		if (peekBuffer.Count > 0)
		{
			// Prioritize consuming from the internal peekBuffer.
			c = peekBuffer.Dequeue();
		}
		else
		{
			// Fallback to reading directly from the underlying stream.
			c = (char)reader.Read();
		}

		// Increment column for a regular character.
		ColumnNumber++;
		return c;
	}

	/// <summary>
	///     Retrieves a specified number of characters from the stream as a list and advances the position.
	/// </summary>
	/// <param name="count">The number of characters to retrieve.</param>
	/// <returns>A list containing the retrieved characters.</returns>
	/// <exception cref="ArgumentException">Thrown if the character count is negative.</exception>
	/// <exception cref="CharStreamException">Thrown if there are not enough characters in the stream to satisfy the request.</exception>
	public List<char> Next(int count)
	{
		if (count < 0)
		{
			throw new ArgumentException("Character count cannot be negative", nameof(count));
		}

		List<char> result = new();
		for (int i = 0; i < count; i++)
		{
			// By calling the single-character Next() method, we ensure that
			// all newline handling and position updates are consistently applied.
			result.Add(Next());
		}

		return result;
	}

	/// <summary>
	///     Peeks at the next character in the stream without consuming it.
	/// </summary>
	/// <returns>The next character in the stream.</returns>
	/// <exception cref="CharStreamException">Thrown if the end of the stream is encountered.</exception>
	public char Peek()
	{
		if (peekBuffer.Count > 0)
		{
			return peekBuffer.Peek();
		}

		if (reader.EndOfStream)
		{
			throw new CharStreamException($"End of stream at line {LineNumber}, column {ColumnNumber}", LineNumber,
				ColumnNumber);
		}

		char next = (char)reader.Read();
		peekBuffer.Enqueue(next);
		return next;
	}

	/// <summary>
	///     Peeks at a specified number of characters in the stream as a list without consuming them.
	/// </summary>
	/// <param name="count">The number of characters to peek at.</param>
	/// <returns>A list containing the peeked characters.</returns>
	/// <exception cref="ArgumentException">Thrown if the character count is negative.</exception>
	/// <exception cref="CharStreamException">Thrown if there are not enough characters in the stream to satisfy the request.</exception>
	public List<char> Peek(int count)
	{
		if (count < 0)
		{
			throw new ArgumentException("Character count cannot be negative");
		}

		List<char> result = new();
		while (peekBuffer.Count < count && !reader.EndOfStream)
		{
			peekBuffer.Enqueue((char)reader.Read());
		}

		if (peekBuffer.Count < count)
		{
			throw new CharStreamException(
				$"Not enough characters in stream (requested {count}, found {peekBuffer.Count}) at line {LineNumber}, column {ColumnNumber}",
				LineNumber, ColumnNumber);
		}

		return peekBuffer.Take(count).ToList();
	}

	/// <summary>
	///     Checks if there are at least a specified number of characters remaining in the stream.
	/// </summary>
	/// <param name="desiredCount"></param>
	/// <returns>readable characters, up to desiredCount</returns>
	private int HasCharsCount(int desiredCount)
	{
		if (desiredCount < 0)
		{
			return 0;
		}

		while (peekBuffer.Count < desiredCount && !reader.EndOfStream)
		{
			peekBuffer.Enqueue((char)reader.Read());
		}

		return peekBuffer.Count < desiredCount ? peekBuffer.Count : desiredCount;
	}

	/// <summary>
	///     Checks if there are at least a specified number of characters remaining in the stream.
	///     This method does not consume any characters.
	/// </summary>
	/// <param name="count">The minimum number of characters to check for.</param>
	/// <returns>True if there are at least 'count' characters available; otherwise, false.</returns>
	public bool HasChars(int count)
	{
		if (count < 0)
		{
			return false;
		}

		while (peekBuffer.Count < count && !reader.EndOfStream)
		{
			peekBuffer.Enqueue((char)reader.Read());
		}

		return peekBuffer.Count >= count;
	}
}