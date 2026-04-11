namespace BMTP3.Common.MessageFormatterParser.Nodes;

public class NamedPlaceholderNode : PlaceholderNode
{
	public NamedPlaceholderNode(string name) : base(NodeType.NamedPlaceholder)
	{
		Name = name;
	}

	public string Name { get; }
}