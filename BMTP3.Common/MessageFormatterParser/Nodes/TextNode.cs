namespace BMTP3.Common.MessageFormatterParser.Nodes
{
	// Text node for plain text
	public class TextNode : AstNode
	{
		public string Value { get; }

		public TextNode(string value) : base(NodeType.Text)
		{
			Value = value;
		}
	}
}
