namespace BMTP3.Common.MessageFormatterParser
{
	// Token types
	public enum TokenType
	{
		LiteralString,      // Raw strings, including text outside expressions and custom-pattern/eval-pattern (e.g., "some text ", "yyyy-MM-dd")
		DollarBraceOpen,    // "${"
		IndexBraceOpen,     // "#{"
		HashBraceOpen,      // "#{"
		BraceClose,         // "}"
		Identifier,         // [a-z,A-Z,_][a-z,A-Z,_,0-9]* (e.g., date, toUpper, number)
		LiteralInteger,     // [0-9]+ (e.g., 123)
		LiteralPattern,     // For format patterns like "yyyy-MM-dd"
		Dot,                // "."
		ParenOpen,          // "("
		ParenClose,         // ")"
		Comma,              // ","
		Colon,              // ":"
		Section,            // "§" and "¶" in eval expressions
		Whitespace,         // Spaces, tabs, newlines (e.g., " ")
		EOF,                // End of file
		QuestionMark        // For ternary operator and if.
	}
}
