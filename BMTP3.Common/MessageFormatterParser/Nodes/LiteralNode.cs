using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Common.MessageFormatterParser.Nodes {
	// Literal node for escaped braces ({{ or }})
	public class LiteralNode : AstNode {
		public string Value { get; }

		public LiteralNode(string value) : base(NodeType.Literal) {
			Value = value;
		}
	}
}
