namespace BMTP3.Common.MessageFormatterParser
{
	public interface ILexer
	{
		// Scans and returns the next token (never null, returns EOF token at end)
		public Token NextToken();

		// Checks if there are more tokens to scan
		public bool HasNextToken();
	}
}
