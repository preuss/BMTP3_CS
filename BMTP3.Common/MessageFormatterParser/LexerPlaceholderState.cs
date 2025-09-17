using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Common.MessageFormatterParser {
	public enum LexerPlaceholderState {
		Argument,           // Parsing Identifier (name) or LiteralInteger (index)
		Function,           // Parsing .identifier() or .toUpper()
		FormatOptions,      // Parsing ,type[,style]
		CustomPattern,      // Parsing :custom-pattern
		EvalType,           // Parsing §evalType
		EvalPattern,        // Parsing eval-pattern (comma-separated, [], or {})
	}
}
