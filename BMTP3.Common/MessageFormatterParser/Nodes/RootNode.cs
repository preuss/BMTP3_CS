namespace BMTP3.Common.MessageFormatterParser.Nodes
{
	// Root node containing a list of children
	public class RootNode : AstNode
	{
		public List<AstNode> Children { get; } = new List<AstNode>();

		public RootNode() : base(NodeType.Root) { }
	}
}
