namespace BMTP3.Common.MessageFormatterParser.Nodes;

// Base class for AST nodes
public abstract class AstNode
{
	protected AstNode(NodeType type)
	{
		Type = type;
	}

	public NodeType Type { get; }
}