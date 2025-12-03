namespace BMTP3.Common.MessageFormatterParser.Nodes
{
	public class IndexedPlaceholderNode : PlaceholderNode
	{
		public int Index { get; }

		public IndexedPlaceholderNode(int index) : base(/*NodeType.IndexedPlaceholder*/ "")
		{
			Index = index;
		}
	}
}
