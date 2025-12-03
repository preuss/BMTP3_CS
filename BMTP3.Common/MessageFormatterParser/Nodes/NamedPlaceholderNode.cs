namespace BMTP3.Common.MessageFormatterParser.Nodes
{
	public class NamedPlaceholderNode : PlaceholderNode
	{
		public string Name { get; }

		public NamedPlaceholderNode(string name) : base(NodeType.NamedPlaceholder)
		{
			Name = name;
		}
	}
}
