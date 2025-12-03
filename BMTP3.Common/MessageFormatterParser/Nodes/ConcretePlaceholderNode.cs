namespace BMTP3.Common.MessageFormatterParser.Nodes;
// Helper class to bypass the abstract/broken hierarchy for now
public class ConcretePlaceholderNode : PlaceholderNode
{
	public ConcretePlaceholderNode(string name, List<FunctionCallNode> funcs, List<AstNode>? pattern, IfConditionNode? cond)
		: base(name, null, null, pattern, cond)
	{
		Functions.AddRange(funcs);
	}
}