using System.Text;

namespace BMTP3.Common.MessageFormatterParser
{
	public abstract class AbstractLexer : ILexer
	{
		protected static readonly HashSet<string> knownFunctions = new HashSet<string>
{
			"toUpper", "toLower", "trim", "abs", "round", "toString"
		};
		protected static readonly HashSet<string> knownTypes = new HashSet<string>
		{
			"number", "string", "date", "if"
		};
		protected static readonly HashSet<string> knownEvalTypes = new HashSet<string>
		{
			"eq0", "gt0", "gte0", "lt0", "lte0", "ne0", "in", "nin"
		};
		protected static readonly HashSet<string> knownStyles = new HashSet<string>
		{
			"decimal", "currency", "percent", "date", "time", "datetime"
		};

		protected readonly CharStream _charStream;

		protected LexerState _state;
		protected readonly StringBuilder _buffer;
		protected int _tokenPosition;
		protected char _lastChar;
		protected bool _inPlaceholder;
		protected AbstractLexer(CharStream charStream)
		{
			_charStream = charStream ?? throw new ArgumentNullException(nameof(charStream), "CharStream cannot be null.");

			_state = LexerState.Base;
			_buffer = new StringBuilder();
			_tokenPosition = 0;
			_lastChar = '\0';
			_inPlaceholder = false;
		}
		protected AbstractLexer(string input) : this(new CharStream(input)) { }

		protected LexerState CurrentState() => _state;
		protected bool HasNextChar() => _charStream.HasChars(1);
		protected char NextChar() => _charStream.Next();
		protected char PeekNextChar() => _charStream.Peek();
		protected bool HasNextChars(int count) => _charStream.HasChars(count);
		protected List<char> NextChars(int count) => _charStream.Next(count);
		protected List<char> PeekNextChars(int count) => _charStream.Peek(count);
		protected bool IsEOF() => _charStream.EndOfStream;
		protected int CurrentLineNumber() => _charStream.LineNumber;
		protected int CurrentColumnNumber() => _charStream.ColumnNumber;

		/*
// Helper: Scans tokens in Initial state
private Token ScanInitial();

// Helper: Scans tokens in Placeholder state
private Token ScanPlaceholder();

// Helper: Scans tokens in EscapedBrace state
private Token ScanEscapedBrace();

// Helper: Checks if a character is a special delimiter in Initial state
private bool IsSpecialChar(char c);

// Helper: Checks if a character is a delimiter in Placeholder state
private bool IsPlaceholderDelimiter(char c);
*/

		public abstract Token NextToken();
		public abstract bool HasNextToken();


	}
}
