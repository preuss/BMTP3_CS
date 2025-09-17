using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Common.MessageFormatterParser {
	// AST node types
	public enum NodeType {
		Root, Text, Placeholder, IndexedPlaceholder, NamedPlaceholder, FunctionCall, Pattern, IfCondition, Literal
	}
}
