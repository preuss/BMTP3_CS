using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Common.MessageFormatterParser.Nodes {
	// Pattern node for custom patterns (e.g., yyyy,MM/dd:EEE)
	public class PatternNode : AstNode {
		public List<AstNode> Content { get; }

		public PatternNode(List<AstNode> content) : base(NodeType.Pattern) {
			Content = content;
		}
	}
}
