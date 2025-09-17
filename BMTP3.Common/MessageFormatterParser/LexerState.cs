using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Common.MessageFormatterParser {
	public enum LexerState {
		Base,            // Starting state, expecting text or special characters from index 0
		Text,               // Collecting LiteralString or placeholder content
		Dollar,             // Seen $, expecting { or text
		Hash,               // Seen #, expecting { or text
		BraceOpen,          // Seen {, expecting { or } or text
		Placeholder,        // Inside ${...} or #{...}, parsing expression content
		EscapedBrace,       // Handling {{ or }} in LiteralString or text
		EOF,                // End of file
	}
}
