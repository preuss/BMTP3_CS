using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Common.MessageFormatterParser.Nodes {
	// Root node containing a list of children
	public class RootNode : AstNode {
		public List<AstNode> Children { get; } = new List<AstNode>();

		public RootNode() : base(NodeType.Root) { }
	}
}
