namespace BMTP3.Common.MessageFormatterParser.Nodes;

public class IndexedPlaceholderNode : PlaceholderNode
{
	public IndexedPlaceholderNode(int index) : base( /*NodeType.IndexedPlaceholder*/ "")
	{
		Index = index;
	}

	public int Index { get; }
}