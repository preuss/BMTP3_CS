using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Common.MessageFormatterParser.Nodes {
	// Base class for AST nodes
	public abstract class AstNode {
		public NodeType Type { get; }

		protected AstNode(NodeType type) {
			Type = type;
		}
	}
}
