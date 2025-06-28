using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Common.MessageFormatterParser {
	public class CharStreamException : Exception {
		public int LineNumber { get; }
		public int ColumnNumber { get; }
		public CharStreamException(string message, int lineNumber, int columnNumber) : base(message) {
			this.LineNumber = lineNumber;
			this.ColumnNumber = columnNumber;
		}
	}
}
