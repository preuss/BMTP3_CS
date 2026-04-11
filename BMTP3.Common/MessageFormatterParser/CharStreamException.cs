namespace BMTP3.Common.MessageFormatterParser;

public class CharStreamException : Exception
{
	public CharStreamException(string message, int lineNumber, int columnNumber) : base(message)
	{
		LineNumber = lineNumber;
		ColumnNumber = columnNumber;
	}

	public int LineNumber { get; }
	public int ColumnNumber { get; }
}