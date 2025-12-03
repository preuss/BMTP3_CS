namespace BMTP3.Common.MessageFormatterParser.Nodes
{
	// Function call node (e.g., .toUpper(4,5))
	public class FunctionCallNode : AstNode
	{
		public string Name { get; }
		public List<AstNode> Arguments { get; }

		public FunctionCallNode(string name, List<AstNode> arguments)
			: base(NodeType.FunctionCall)
		{
			Name = name;
			Arguments = arguments;
		}
	}
}
