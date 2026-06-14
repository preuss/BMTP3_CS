using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace BMTP3.Core4.Utilities;

/// <summary>
/// v4.1: A utility class for matching file paths against glob patterns, supporting .gitignore-style semantics.
/// Matches file-system-like paths against glob patterns.
///
/// Supported syntax:
/// - *          : zero or more characters except path separators
/// - ?          : exactly one character except path separators
/// - **         : zero or more directory segments
/// - [abc]      : character class
/// - [a-z]      : character range
/// - [!abc]     : negated character class
/// - {a,b}      : simple brace expansion
/// - @(a|b)     : exactly one alternative
/// - *(a|b)     : zero or more repetitions
/// - +(a|b)     : one or more repetitions
/// - ?(a|b)     : zero or one repetition
/// - !(pattern) : global negation
/// - *.!(ext)   : suffix negation
///
/// Rules:
/// - Matching is case-insensitive and culture-invariant.
/// - Both '/' and '\' are treated as path separators.
/// - This is a practical glob matcher, not a full Bash parser.
/// - Nested brace expansion and complex nested extglobs are intentionally not supported.
///
/// Error handling:
/// - Exclude evaluation fails closed: an error rejects the path.
/// - Include evaluation is tolerant: an error skips the current pattern and continues.
/// </summary>
internal static class GlobMatcher
{
	private const string SeparatorRegex = @"[/\\]+";

	private static readonly RegexOptions DefaultRegexOptions =
		RegexOptions.IgnoreCase |
		RegexOptions.CultureInvariant |
		RegexOptions.Compiled;

	// A timeout reduces the risk of pathological regex execution consuming too much CPU.
	private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(200);

	// Use a case-insensitive comparer because matching itself is case-insensitive.
	private static readonly ConcurrentDictionary<string, Regex> RegexCache =
		new(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Returns <c>true</c> when <paramref name="path"/> should be processed
	/// according to include and exclude patterns.
	///
	/// Rules:
	/// 1. If any exclude pattern matches, the path is excluded.
	/// 2. If an exclude pattern errors or times out, the path is excluded.
	/// 3. If include patterns are provided, at least one non-blank, valid include pattern must match.
	/// 4. If an include pattern errors or times out, it is ignored and evaluation continues.
	/// 5. Blank include patterns are ignored.
	/// 6. If no non-blank include patterns exist, the path is included by default.
	/// </summary>
	public static bool IsIncluded(
		string path,
		IEnumerable<string>? includePatterns,
		IEnumerable<string>? excludePatterns)
	{
		ArgumentNullException.ThrowIfNull(path);

		string normalizedPath = NormalizePath(path);

		// Exclude evaluation is fail-closed.
		if(MatchesAnyNormalizedPath(normalizedPath, excludePatterns, failClosedOnError: true))
		{
			return false;
		}

		if(includePatterns is null)
		{
			return true;
		}

		bool hasIncludePatterns = false;

		foreach(string pattern in includePatterns)
		{
			if(string.IsNullOrWhiteSpace(pattern))
			{
				continue;
			}

			hasIncludePatterns = true;

			MatchResult result = MatchNormalizedPath(normalizedPath, pattern);

			if(result == MatchResult.Error)
			{
				// Ignore invalid or timed-out include patterns and continue evaluating
				// the remaining include list. Inclusion is additive: one good match is enough.
				continue;
			}

			if(result == MatchResult.Match)
			{
				return true;
			}
		}

		return !hasIncludePatterns;
	}

	/// <summary>
	/// Returns <c>true</c> when <paramref name="path"/> matches <paramref name="pattern"/>.
	///
	/// If regex evaluation times out or regex construction fails, the method returns <c>false</c>.
	/// </summary>
	public static bool Matches(string path, string pattern)
	{
		ArgumentNullException.ThrowIfNull(path);
		ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

		string normalizedPath = NormalizePath(path);
		return MatchNormalizedPath(normalizedPath, pattern) == MatchResult.Match;
	}

	/// <summary>
	/// Converts a glob pattern into an anchored regex pattern string.
	/// </summary>
	public static string GlobToRegex(string pattern)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

		string glob = NormalizePattern(pattern);
		glob = ExpandSimpleBraces(glob);

		if(TryBuildLeadingRecursiveRegex(glob, out string? regex))
		{
			return regex;
		}

		if(TryBuildGlobalNegationRegex(glob, out regex))
		{
			return regex;
		}

		if(TryBuildSuffixNegationRegex(glob, out regex))
		{
			return regex;
		}

		return ConvertCore(glob, anchor: true);
	}

	private static bool MatchesAnyNormalizedPath(
		string normalizedPath,
		IEnumerable<string>? patterns,
		bool failClosedOnError)
	{
		if(patterns is null)
		{
			return false;
		}

		foreach(string pattern in patterns)
		{
			if(string.IsNullOrWhiteSpace(pattern))
			{
				continue;
			}

			MatchResult result = MatchNormalizedPath(normalizedPath, pattern);

			if(result == MatchResult.Error)
			{
				return failClosedOnError;
			}

			if(result == MatchResult.Match)
			{
				return true;
			}
		}

		return false;
	}

	private static MatchResult MatchNormalizedPath(string normalizedPath, string pattern)
	{
		try
		{
			Regex regex = GetOrCreateRegex(pattern);

			return regex.IsMatch(normalizedPath)
				? MatchResult.Match
				: MatchResult.NoMatch;
		} catch(RegexMatchTimeoutException)
		{
			return MatchResult.Error;
		} catch(ArgumentException)
		{
			return MatchResult.Error;
		}
	}

	private static Regex GetOrCreateRegex(string pattern)
	{
		string normalizedPattern = NormalizePattern(pattern);
		return RegexCache.GetOrAdd(normalizedPattern, CreateRegex);
	}

	private static Regex CreateRegex(string normalizedPattern)
	{
		string regexPattern = GlobToRegex(normalizedPattern);
		return new Regex(regexPattern, DefaultRegexOptions, RegexTimeout);
	}

	private static string NormalizePath(string path)
	{
		return path.Replace('\\', '/');
	}

	private static string NormalizePattern(string pattern)
	{
		return pattern.Replace('\\', '/');
	}

	/// <summary>
	/// Expands simple brace expressions like "{a,b,c}" into "@(a|b|c)".
	/// This intentionally supports only flat, comma-separated alternatives.
	/// </summary>
	private static string ExpandSimpleBraces(string glob)
	{
		return Regex.Replace(
			glob,
			@"\{([^{},]+(?:,[^{}]+)+)\}",
			match =>
			{
				string[] alternatives = match.Groups[1].Value
					.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

				return "@(" + string.Join("|", alternatives) + ")";
			});
	}

	/// <summary>
	/// Handles the special case where the pattern starts with "**/".
	/// This should match both nested paths and files directly at the root.
	/// </summary>
	private static bool TryBuildLeadingRecursiveRegex(string glob, out string? regex)
	{
		regex = null;

		if(!glob.StartsWith("**/", StringComparison.Ordinal))
		{
			return false;
		}

		string remainder = glob.Substring(3);
		regex = "^(?:.*(?:/|\\\\))?" + ConvertCore(remainder, anchor: false) + "$";
		return true;
	}

	/// <summary>
	/// Handles global negation like "!(pattern)".
	/// </summary>
	private static bool TryBuildGlobalNegationRegex(string glob, out string? regex)
	{
		regex = null;

		if(!glob.StartsWith("!(", StringComparison.Ordinal) || !glob.EndsWith(')'))
		{
			return false;
		}

		int end = FindMatchingParenthesis(glob, 1);
		if(end != glob.Length - 1)
		{
			return false;
		}

		string positivePattern = glob.Substring(2, glob.Length - 3);
		string positiveRegex = ConvertCore(positivePattern, anchor: false);

		regex = $"^(?!(?:{positiveRegex})$).*$";
		return true;
	}

	/// <summary>
	/// Handles suffix negation like "*.!(jpg)".
	/// </summary>
	private static bool TryBuildSuffixNegationRegex(string glob, out string? regex)
	{
		regex = null;

		int negationStart = glob.LastIndexOf("!(", StringComparison.Ordinal);
		if(negationStart <= 0 || !glob.EndsWith(')'))
		{
			return false;
		}

		int openParenIndex = negationStart + 1;
		int negationEnd = FindMatchingParenthesis(glob, openParenIndex);

		if(negationEnd != glob.Length - 1)
		{
			return false;
		}

		string prefixGlob = glob.Substring(0, negationStart);
		string negatedSuffixGlob = glob.Substring(negationStart + 2, negationEnd - (negationStart + 2));

		string prefixRegex = ConvertCore(prefixGlob, anchor: false);
		string negatedSuffixRegex = ConvertCore(negatedSuffixGlob, anchor: false);

		regex = $"^{prefixRegex}(?!(?:{negatedSuffixRegex})$).*$";
		return true;
	}

	/// <summary>
	/// Converts the core glob syntax into regex syntax.
	/// </summary>
	private static string ConvertCore(string glob, bool anchor)
	{
		string regex = Regex.Escape(glob);

		// Restore character classes and convert glob-style negation.
		regex = regex.Replace(@"\[", "[").Replace(@"\]", "]");
		regex = Regex.Replace(regex, @"\[!(.+?)\]", "[^$1]");

		// Convert practical extglobs.
		regex = Regex.Replace(regex, @"\\\*\\\((.+?)\\\)", m => $"(?:{m.Groups[1].Value.Replace(@"\|", "|")})*");
		regex = Regex.Replace(regex, @"@\\\((.+?)\\\)", m => $"(?:{m.Groups[1].Value.Replace(@"\|", "|")})");
		regex = Regex.Replace(regex, @"\\\+\\\((.+?)\\\)", m => $"(?:{m.Groups[1].Value.Replace(@"\|", "|")})+");
		regex = Regex.Replace(regex, @"\\\?\\\((.+?)\\\)", m => $"(?:{m.Groups[1].Value.Replace(@"\|", "|")})?");

		// Convert literal path separators into a separator-tolerant regex.
		regex = Regex.Replace(regex, @"\\{2}", SeparatorRegex);
		regex = regex.Replace("/", SeparatorRegex);

		// Convert recursive directory matching.
		string recursiveDirectoryMatcher = $@"(?:[^/\\]*{SeparatorRegex})*";
		regex = Regex.Replace(regex, @"\*\*" + SeparatorRegex, recursiveDirectoryMatcher);

		// Convert remaining recursive wildcards.
		regex = regex.Replace(@"\*\*", ".*");

		// Convert standard wildcards.
		regex = regex.Replace(@"\*", @"[^/\\]*");

		// '?' means exactly one non-separator character.
		regex = regex.Replace(@"\?", @"[^/\\]");

		return anchor ? $"^{regex}$" : regex;
	}

	/// <summary>
	/// Finds the matching closing parenthesis for the opening parenthesis at <paramref name="openIndex"/>.
	/// Returns -1 if no valid match exists.
	/// </summary>
	private static int FindMatchingParenthesis(string value, int openIndex)
	{
		if(openIndex < 0 || openIndex >= value.Length || value[openIndex] != '(')
		{
			return -1;
		}

		int depth = 0;

		for(int i = openIndex; i < value.Length; i++)
		{
			char c = value[i];

			if(c == '(')
			{
				depth++;
			} else if(c == ')')
			{
				depth--;

				if(depth == 0)
				{
					return i;
				}
			}
		}

		return -1;
	}

	private enum MatchResult
	{
		NoMatch = 0,
		Match = 1,
		Error = 2
	}
}