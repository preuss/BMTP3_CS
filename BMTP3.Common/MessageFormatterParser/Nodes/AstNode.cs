namespace BMTP3.Common.MessageFormatterParser.Nodes
{
	// Base class for AST nodes
	public abstract class AstNode
	{
		public NodeType Type { get; }

		protected AstNode(NodeType type)
		{
			Type = type;
		}
	}
}
