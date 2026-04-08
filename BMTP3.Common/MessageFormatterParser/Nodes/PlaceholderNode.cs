namespace BMTP3.Common.MessageFormatterParser.Nodes;

// Placeholder node for ${name} or #{index}
public abstract class PlaceholderNode : AstNode
{
	public PlaceholderNode(string nameOrIndex, string? type = null, string? style = null,
		List<AstNode>? pattern = null, IfConditionNode? condition = null)
		: base(NodeType.Placeholder)
	{
		NameOrIndex = nameOrIndex;
		Type = type;
		Style = style;
		Pattern = pattern;
		Condition = condition;
	}

	public PlaceholderNode(NodeType nodeType) : this(nodeType.ToString())
	{
	}

	public string NameOrIndex { get; }
	public List<FunctionCallNode> Functions { get; } = new();
	public new string? Type { get; }
	public string? Style { get; }
	public List<AstNode>? Pattern { get; }
	public IfConditionNode? Condition { get; }
}