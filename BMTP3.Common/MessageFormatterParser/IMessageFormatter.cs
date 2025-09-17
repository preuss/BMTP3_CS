using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Common.MessageFormatterParser {
	public interface IMessageFormatter {
		public string Format(string template, Dictionary<string, object> values);
	}
}
