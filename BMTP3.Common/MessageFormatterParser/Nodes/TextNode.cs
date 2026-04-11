namespace BMTP3.Common.MessageFormatterParser.Nodes;

// Text node for plain text
public class TextNode : AstNode
{
	public TextNode(string value) : base(NodeType.Text)
	{
		Value = value;
	}

	public string Value { get; }
}