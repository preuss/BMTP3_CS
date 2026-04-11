namespace BMTP3.Common.MessageFormatterParser;

// Represents a single token
public class Token
{
	public Token(TokenType type, string value, int position, int line, int column)
	{
		Type = type;
		Value = value;
		Position = position;
		Line = line;
		Column = column;
	}

	public Token(TokenType type, char value, int position, int line, int column) : this(type, value.ToString(),
		position, line, column)
	{
	}

	public TokenType Type { get; }
	public string Value { get; }
	public int Position { get; }
	public int Line { get; }
	public int Column { get; }

	public override string ToString()
	{
		return $"[{Type}: '{Value}' @ Line {Line}, Column {Column}, Position {Position}]";
	}
}