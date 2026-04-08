namespace BMTP3.Common.MessageFormatterParser.Nodes;

// Root node containing a list of children
public class RootNode : AstNode
{
	public RootNode() : base(NodeType.Root)
	{
	}

	public List<AstNode> Children { get; } = new();
}