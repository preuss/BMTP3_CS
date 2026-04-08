namespace BMTP3.Common.MessageFormatterParser.Nodes;

// Pattern node for custom patterns (e.g., yyyy,MM/dd:EEE)
public class PatternNode : AstNode
{
	public PatternNode(List<AstNode> content) : base(NodeType.Pattern)
	{
		Content = content;
	}

	public List<AstNode> Content { get; }
}