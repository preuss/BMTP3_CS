using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Common.MessageFormatterParser.Nodes {
	// Text node for plain text
	public class TextNode : AstNode {
		public string Value { get; }

		public TextNode(string value) : base(NodeType.Text) {
			Value = value;
		}
	}
}
