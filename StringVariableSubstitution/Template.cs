using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BMTP3_CS.StringVariableSubstitution {
	class Template {
		private readonly IDictionary<string, string> variables = new Dictionary<string, string>();
		public void AddVariable(string name, string value) {
			if(string.IsNullOrEmpty(name)) {
				throw new ArgumentException("Variable name cannot be null or empty", nameof(name));
			}
			variables[name] = value;
		}
		public void AddVariable(string name, int value) {
			AddVariable(name, value.ToString());
		}
		public void RemoveVariable(string name) {
			if(string.IsNullOrEmpty(name)) {
				throw new ArgumentException("Variable name cannot be null or empty", nameof(name));
			}
			variables.Remove(name);
		}
		public string Replace(string input) {
			StringBuilder builder = new StringBuilder(input);
			foreach(var variable in variables) {
				//input = Regex.Replace(input, @"\${" + variable.Key + "}", variable.Value);
				builder.Replace($"${{{variable.Key}}}", variable.Value);
			}
			return builder.ToString();
		}
	}
}
