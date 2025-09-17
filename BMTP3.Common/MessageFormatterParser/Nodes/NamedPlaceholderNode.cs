using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Common.MessageFormatterParser.Nodes {
	public class NamedPlaceholderNode : PlaceholderNode {
		public string Name { get; }

		public NamedPlaceholderNode(string name) : base(NodeType.NamedPlaceholder) {
			Name = name;
		}
	}
}
