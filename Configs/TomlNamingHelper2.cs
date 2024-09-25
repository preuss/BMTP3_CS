using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tomlyn.Helpers;

namespace BMTP3_CS.Configs {
	public static class TomlNamingHelper2 {
		public static string PascalCaseToCamelCase(string name) {
			if(string.IsNullOrEmpty(name)) return name;

			// Konverter første bogstav til småt
			var firstLetter = char.ToLowerInvariant(name[0]);

			// Hvis navnet kun har ét bogstav, returner det konverterede bogstav
			if(name.Length == 1) return firstLetter.ToString();

			// Ellers, returner det konverterede første bogstav plus resten af strengen
			//return firstLetter + name.Substring(1);
			return $"{firstLetter}{name.Substring(1)}";
		}
		public static string CamelCaseToPascalCase(string name) {
			if(string.IsNullOrEmpty(name)) return name;

			// Konverter første bogstav til stort
			var firstLetter = char.ToUpperInvariant(name[0]);

			// Hvis navnet kun har ét bogstav, returner det konverterede bogstav
			if(name.Length == 1) return firstLetter.ToString();

			// Ellers, returner det konverterede første bogstav plus resten af strengen
			//return firstLetter + name.Substring(1);
			return $"{firstLetter}{name.Substring(1)}";
		}
	}
}
