using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Common.MessageFormatterParser {
	public enum TokenSubType {
		None,               // Generic identifier
		Name,               // Identifier as name (e.g., date)
		Type,               // Identifier as type (e.g., number)
		Style,              // Identifier as style (e.g., decimal)
		EvalType,           // Identifier as evalType (e.g., eq0)
	}
}
