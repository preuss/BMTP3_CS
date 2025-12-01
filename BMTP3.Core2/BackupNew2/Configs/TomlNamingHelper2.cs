using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.Configs {
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
		public static string PascalCaseToCamelCase(string? name) {
			ArgumentNullException.ThrowIfNull(name);
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
		public static string CamelCaseToPascalCase(string? name) {
			ArgumentNullException.ThrowIfNull(name);
			if(string.IsNullOrEmpty(name)) return name;

			// Convert first letter to uppercase
			char firstLetter = char.ToUpperInvariant(name[0]);

			// If the name has only one letter, return the converted letter
			if(name.Length == 1) return firstLetter.ToString();

			// Otherwise, return the converted first letter plus the rest of the string
			return $"{firstLetter}{name.Substring(1)}";
		}
		/// <summary>
		/// Converts a PascalCase string to a snake_case string.
		/// </summary>
		/// <param name="name">The PascalCase string to convert. Cannot be null.</param>
		/// <returns>A snake_case representation of the input string.</returns>
		public static string PascalCaseToSnakeCase(string? name) {
			ArgumentNullException.ThrowIfNull(name);

			// Simple, thread-safe approach
			StringBuilder result = new();
			char pc = (char)0;

			foreach(char c in name) {
				if(char.IsUpper(c) && !char.IsUpper(pc) && pc != 0 && pc != '_') {
					result.Append('_');
				}
				result.Append(char.ToLowerInvariant(c));
				pc = c;
			}

			return result.ToString();
		}
		/// <summary>
		/// Converts a snake_case string to PascalCase.
		/// </summary>
		/// <param name="name">The snake_case string to convert. Cannot be null.</param>
		/// <returns>A PascalCase representation of the input string.</returns>
		public static string SnakeCaseToPascalCase(string? name) {
			ArgumentNullException.ThrowIfNull(name);
			if(string.IsNullOrEmpty(name)) return name;

			StringBuilder result = new();
			bool capitalizeNext = true; // First letter should be capitalized

			foreach(char c in name) {
				if(c == '_') {
					// Skip underscore and capitalize next letter
					capitalizeNext = true;
				} else {
					if(capitalizeNext) {
						result.Append(char.ToUpperInvariant(c));
						capitalizeNext = false;
					} else {
						result.Append(char.ToLowerInvariant(c));
					}
				}
			}

			return result.ToString();
		}
		/// <summary>
		/// Converts a snake_case string to camelCase.
		/// </summary>
		/// <param name="name">The snake_case string to convert. Cannot be null.</param>
		/// <returns>A camelCase representation of the input string.</returns>
		public static string SnakeCaseToCamelCase(string? name) {
			ArgumentNullException.ThrowIfNull(name);
			if(string.IsNullOrEmpty(name)) return name;

			StringBuilder result = new();
			bool capitalizeNext = false; // First letter should be lowercase for camelCase

			foreach(char c in name) {
				if(c == '_') {
					capitalizeNext = true;
				} else {
					if(capitalizeNext) {
						result.Append(char.ToUpperInvariant(c));
						capitalizeNext = false;
					} else {
						result.Append(char.ToLowerInvariant(c));
					}
				}
			}

			return result.ToString();
		}
	}
}
