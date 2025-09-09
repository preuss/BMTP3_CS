using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tomlyn.Helpers;

namespace BMTP3.Core.Configs {
	/// <summary>
	/// Helper class for converting between different naming conventions used in TOML configuration files.
	/// Provides utility methods for converting between PascalCase and camelCase naming conventions.
	/// </summary>
	public static class TomlNamingHelper2 {
		/// <summary>
		/// Converts a PascalCase string to camelCase.
		/// </summary>
		/// <param name="name">The PascalCase string to convert</param>
		/// <returns>The converted camelCase string</returns>
		public static string PascalCaseToCamelCase(string name) {
			if(string.IsNullOrEmpty(name)) return name;

			// Convert first letter to lowercase
			char firstLetter = char.ToLowerInvariant(name[0]);

			// If the name has only one letter, return the converted letter
			if(name.Length == 1) return firstLetter.ToString();

			// Otherwise, return the converted first letter plus the rest of the string
			return $"{firstLetter}{name.Substring(1)}";
		}
		/// <summary>
		/// Converts a camelCase string to PascalCase.
		/// </summary>
		/// <param name="name">The camelCase string to convert</param>
		/// <returns>The converted PascalCase string</returns>
		public static string CamelCaseToPascalCase(string name) {
			if(string.IsNullOrEmpty(name)) return name;

			// Convert first letter to uppercase
			char firstLetter = char.ToUpperInvariant(name[0]);

			// If the name has only one letter, return the converted letter
			if(name.Length == 1) return firstLetter.ToString();

			// Otherwise, return the converted first letter plus the rest of the string
			return $"{firstLetter}{name.Substring(1)}";
		}
	}
}
