namespace BMTP3.Common.MessageFormatterParser.Nodes;

// Literal node for escaped braces ({{ or }})
public class LiteralNode : AstNode
{
	public LiteralNode(string value) : base(NodeType.Literal)
	{
		Value = value;
	}

	public string Value { get; }
}