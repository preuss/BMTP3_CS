using System.Text.RegularExpressions;

namespace BMTP3.Core4.Utilities;

/// <summary>
///     Converts glob patterns into compiled <see cref="Regex" /> instances and tests
///     file paths against include/exclude pattern lists.
///     Supported glob syntax:
///     <list type="bullet">
///         <item><c>*</c> — matches any characters except path separators</item>
///         <item><c>?</c> — matches one character except path separators</item>
///         <item><c>**</c> — matches zero or more directory segments</item>
///         <item><c>[abc]</c> / <c>[a-z]</c> — character classes</item>
///         <item><c>[!abc]</c> — negated character classes</item>
///         <item><c>{a,b}</c> — brace expansion: any comma-separated alternative</item>
///         <item><c>@(a|b)</c> — extglob: exactly one of the alternatives</item>
///         <item><c>*(a|b)</c> — extglob: zero or more</item>
///         <item><c>+(a|b)</c> — extglob: one or more</item>
///         <item><c>?(a|b)</c> — extglob: zero or one</item>
///         <item><c>!(pattern)</c> — global negation: anything that does NOT match pattern</item>
///         <item><c>prefix.!(ext)</c> — suffix negation: exclude a specific extension</item>
///     </list>
/// </summary>
public static class GlobMatcher
{
	private const string SeparatorRegex = @"[/\\]+";

	private static readonly RegexOptions DefaultOptions =
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled;

	/// <summary>
	///     Returns <c>true</c> when <paramref name="path" /> should be processed
	///     given the supplied include and exclude pattern lists.
	///     Rules (same as .gitignore-style semantics):
	///     <list type="number">
	///         <item>If <paramref name="excludePatterns" /> is non-empty and any pattern matches → exclude.</item>
	///         <item>If <paramref name="includePatterns" /> is non-empty → include only when at least one pattern matches.</item>
	///         <item>If both lists are empty → include everything.</item>
	///     </list>
	/// </summary>
	public static bool IsIncluded(string path, IEnumerable<string>? includePatterns,
		IEnumerable<string>? excludePatterns)
	{
		// Normalise separators so both / and \ work uniformly against patterns.
		string normPath = path.Replace('\\', '/');

		if(excludePatterns != null)
		{
			foreach(string pattern in excludePatterns)
			{
				if(string.IsNullOrWhiteSpace(pattern))
				{
					continue;
				}

				if(Matches(normPath, pattern))
				{
					return false;
				}
			}
		}

		if(includePatterns != null)
		{
			bool hasAny = false;
			foreach(string pattern in includePatterns)
			{
				if(string.IsNullOrWhiteSpace(pattern))
				{
					continue;
				}

				hasAny = true;
				if(Matches(normPath, pattern))
				{
					return true;
				}
			}

			if(hasAny)
			{
				return false; // had patterns but none matched
			}
		}

		return true;
	}

	/// <summary>
	///     Tests whether <paramref name="path" /> matches a single glob <paramref name="pattern" />.
	/// </summary>
	public static bool Matches(string path, string pattern)
	{
		string regexStr = GlobToRegex(pattern);
		return Regex.IsMatch(path, regexStr, DefaultOptions);
	}

	/// <summary>
	///     Converts a glob pattern string into its equivalent regular-expression string.
	///     The resulting pattern is anchored (<c>^...$</c>) and case-insensitive by default
	///     when used via <see cref="Matches" />.
	/// </summary>
	public static string GlobToRegex(string globPattern)
	{
		// Brace expansion: {a,b,c} → @(a|b|c)
		// Only expands braces containing at least one comma
		// (avoiding conflict with regex-style {n} quantifiers).
		globPattern = Regex.Replace(globPattern, @"\{([^{},]+,[^{}]*)\}", m =>
		{
			string inner = m.Groups[1].Value;
			string alts = string.Join("|", inner.Split(','));
			return "@(" + alts + ")";
		});

		// Special-case: pattern starting with extglob alternation like @(a|b)rest
		if(globPattern.StartsWith("@(") && globPattern.Contains(')'))
		{
			int end = globPattern.IndexOf(')');
			if(end > 2)
			{
				string inner = globPattern.Substring(2, end - 2);
				string remainder = globPattern.Substring(end + 1);
				string remRegex = ConvertCore(remainder, true);
				return $"^({inner}){remRegex}$";
			}
		}

		// Special-case: leading **/ should allow zero or more directory segments
		// including files directly at root (no separator required).
		if(globPattern.StartsWith("**/") || globPattern.StartsWith("**\\"))
		{
			string remainder = globPattern.Substring(3);
			string remRegex = ConvertCore(remainder, true);
			return "^(?:.*(?:/|\\\\))?" + remRegex + "$";
		}

		// Global negation: !(pattern)
		if(globPattern.StartsWith("!(") && globPattern.EndsWith(')'))
		{
			string positivePattern = globPattern.Substring(2, globPattern.Length - 3);
			string positiveRegex = ConvertCore(positivePattern, true).Replace("\\|", "|");
			return $"^(?!(?:{positiveRegex})$).*$";
		}

		// Extended suffix negation: *.!(ext)
		if(globPattern.Contains("!(") && globPattern.EndsWith(')'))
		{
			int negStart = globPattern.LastIndexOf("!(");
			int negEnd = globPattern.LastIndexOf(')');

			if(negStart > 0 && negEnd == globPattern.Length - 1)
			{
				string negSuffix = globPattern.Substring(negStart + 2, negEnd - (negStart + 2));
				string negRegex = Regex.Escape(negSuffix).Replace(@"\*", ".*").Replace(@"\?", ".").Replace("\\|", "|");
				string prefixGlob = globPattern.Substring(0, negStart);
				string prefixRegex = ConvertCore(prefixGlob, true);
				return $"^{prefixRegex}(?!(?:{negRegex})$)(.*)$";
			}
		}

		return ConvertCore(globPattern, false);
	}

	private static string ConvertCore(string glob, bool ignoreAnchors)
	{
		string r = Regex.Escape(glob);

		// Restore character classes [ ] and convert negation [!...] → [^...]
		r = r.Replace(@"\[", "[").Replace(@"\]", "]");
		r = Regex.Replace(r, @"\[!(.+?)\]", "[^$1]");

		// Extglob operators — unescape | inside groups so alternation works
		r = Regex.Replace(r, @"\\\*\\\((.+?)\\\)", m => $"({m.Groups[1].Value.Replace("\\|", "|")})*");
		r = Regex.Replace(r, @"@\\\((.+?)\\\)", m => $"({m.Groups[1].Value.Replace("\\|", "|")})");
		r = Regex.Replace(r, @"\\\+\\\((.+?)\\\)", m => $"({m.Groups[1].Value.Replace("\\|", "|")})+");
		r = Regex.Replace(r, @"\\\?\\\((.+?)\\\)", m => $"({m.Groups[1].Value.Replace("\\|", "|")})?");

		// Literal path separators → flexible separator regex
		r = Regex.Replace(r, @"\\{2}", SeparatorRegex); // escaped backslash
		r = r.Replace("/", SeparatorRegex); // forward slash

		// **/ → zero-or-more directory segments
		string recursiveMatcher = $@"((?:[^/\\]*{SeparatorRegex})*)";
		r = Regex.Replace(r, @"\*\*" + SeparatorRegex, recursiveMatcher);

		// ** (not followed by separator) → .*
		r = r.Replace(@"\*\*", ".*");

		// * → [^/\\]*   ? → [^/\\]?
		r = r.Replace(@"\*", @"[^/\\]*");
		r = r.Replace(@"\?", @"[^/\\]?");

		return ignoreAnchors ? r : $"^{r}$";
	}
}
