using System.Text.RegularExpressions;

namespace BMTP3.Core2.BackupNew.Utilities; // Updated namespace
public static class GlobConverter
{
	// Regex pattern to match either forward slash (/) or backslash (\) as a path separator.
	private const string SeparatorRegex = @"[\\/\\]";

	/// <summary>
	/// Converts a Glob pattern into a Regex pattern, supporting Standard Globs, 
	/// Recursive (**), all POSIX/Bash Extglobs (@, +, ?, !) and extended suffix negation.
	/// </summary>
	public static string GlobToRegex(string globPattern)
	{
		// 1. Handle Global Negation: !(*.jpg) (Priority for POSIX syntax)
		if(globPattern.StartsWith("!((") && globPattern.EndsWith("))"))
		{
			string positivePattern = globPattern.Substring(3, globPattern.Length - 4);
			// Internal conversion without anchors, as the Lookahead will anchor the entire string.
			string positiveRegex = ConvertCoreGlobToRegex(positivePattern, ignoreAnchors: true);
			// Global negation logic: Match anything that is NOT the positive pattern.
			return $"^(?!{positiveRegex}$).*$";
		}

		// 2. Handle Extended Suffix Negation: *.!(jpg)
		// This implements the requested local Negative Lookahead Suffix Match.
		if(globPattern.Contains("!(") && globPattern.Contains(")") && globPattern.EndsWith("))"))
		{
			int negationStart = globPattern.LastIndexOf("!(");
			int negationEnd = globPattern.LastIndexOf(')');

			if(negationStart > 0 && negationEnd == globPattern.Length - 1)
			{
				string negatedSuffix = globPattern.Substring(negationStart + 2, negationEnd - (negationStart + 2));
				// Convert the suffix to be negated into its Regex form. We only care about literal matching here.
				string negatedRegex = Regex.Escape(negatedSuffix).Replace("*", ".*").Replace("?", ".");

				string prefixGlob = globPattern.Substring(0, negationStart);
				string prefixRegex = ConvertCoreGlobToRegex(prefixGlob, ignoreAnchors: true);

				// Local negation logic: ^prefix(?!suffix$).*
				return $"^{prefixRegex}(?!{negatedRegex}$)(.*)$";
			}
		}

		// 3. Standard and Extended Positive Glob Conversion
		return ConvertCoreGlobToRegex(globPattern, ignoreAnchors: false);
	}

	/// <summary>
	/// Handles the core conversion logic for all supported Glob elements.
	/// </summary>
	private static string ConvertCoreGlobToRegex(string globPattern, bool ignoreAnchors)
	{
		// Define the Regex pattern for path separators (forward or backslash).
		const string SeparatorRegex = @"[\\/\\]";

		// 1. Escape all Regex special characters first.
		string regexPattern = Regex.Escape(globPattern);

		// --- Character Class and Negation Correction ---

		// Remove escaping from [ and ] so they function as a character class in Regex.
		regexPattern = regexPattern.Replace("[", "[").Replace("]", "]");

		// Convert Glob negation inside character classes [!...] to Regex negation [^...].
		regexPattern = Regex.Replace(regexPattern, @"\[!(.+?)\]", @"[^$1]");

		// --- Extglob Operator Conversion (Including *, @, +, ?) ---

		// *(a|b) -> (a|b)*
		regexPattern = Regex.Replace(regexPattern, @"\*\((.+?)\)", "($1)*");

		// @(a|b) -> (a|b)
		regexPattern = Regex.Replace(regexPattern, @"\\@\((.+?)\)", "($1)");

		// +(a|b) -> (a|b)+
		regexPattern = Regex.Replace(regexPattern, @"\\+\((.+?)\)", "($1)+");

		// ?(a|b) -> (a|b)?
		regexPattern = Regex.Replace(regexPattern, @"\\?\((.+?)\)", "($1)?");

		// --- Separator and Recursive Wildcard Conversion ---

		// 1. Convert all literal separators in the pattern to the flexible SeparatorRegex.
		// This handles both C:\ (escaped to C:\\ by Regex.Escape) and C:/ (not escaped).

		// A. Handle Backslash: Replace double backslash (escaped literal \) with [/\]
		regexPattern = Regex.Replace(regexPattern, @"\\{2}", SeparatorRegex);

		// B. Handle Forward Slash: Replace literal forward slash (/) with [/\]
		// This must happen after backslash conversion, as forward slash is not escaped by Regex.Escape.
		regexPattern = regexPattern.Replace("/", SeparatorRegex);

		// 2. ** Conversion:

		// Define the matcher for zero or more directory segments: ((?:[^/\\]*[/\\])*)
		string recursivePathMatcher = $"((?:[^/\\]*{SeparatorRegex})*)";

		// Replace ** followed by the SeparatorRegex. 
		// This is safe now because all literal path separators have been converted to SeparatorRegex.
		regexPattern = Regex.Replace(regexPattern, @"\*\*" + SeparatorRegex, recursivePathMatcher);

		// If ** is not followed by a separator, treat it as a general .*
		regexPattern = regexPattern.Replace(@"\*\*", ".*");

		// --- Standard Wildcard Conversion (MUST be done after Extglobs/**) ---

		// * (Standard Glob) -> [^/\\]* (Matches zero or more characters that are NOT a path separator)
		regexPattern = regexPattern.Replace(@"\*", @"[^/\\]*");

		// ? (Standard Glob) -> [^/\\]? (Matches exactly one character that is NOT a path separator)
		regexPattern = regexPattern.Replace(@"\?", @"[^/\\]?");

		// --- Final Anchors ---

		// Add anchors
		if(!ignoreAnchors)
		{
			regexPattern = $"^{regexPattern}$";
		}

		return regexPattern;
	}
}
