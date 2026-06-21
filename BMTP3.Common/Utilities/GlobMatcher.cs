using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace BMTP3.Common.Utilities;

/// <summary>
/// Specifies which characters <see cref="GlobMatcher"/> treats as path separators.
/// </summary>
public enum GlobSeparatorMode
{
	/// <summary>Both '/' and '\' are treated as path separators.</summary>
	Both = 0,

	/// <summary>Only '/' is treated as a path separator. '\' is a regular character (default).</summary>
	ForwardSlash = 1,

	/// <summary>Only '\' is treated as a path separator. '/' is a regular character.</summary>
	Backslash = 2,
}

/// <summary>
/// v4.15: A utility class for matching file paths against glob patterns, supporting .gitignore-style semantics.
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
/// - <see cref="GlobSeparatorMode"/> controls which characters are treated as separators:
///   <see cref="GlobSeparatorMode.Both"/> — both '/' and '\'.
///   <see cref="GlobSeparatorMode.ForwardSlash"/> (default) — only '/'.
///   <see cref="GlobSeparatorMode.Backslash"/> — only '\'.
/// - This is a practical glob matcher, not a full Bash parser.
/// - Nested brace expansion and complex nested extglobs are intentionally not supported.
///
/// Error handling:
/// - Exclude evaluation fails closed: an error rejects the path.
/// - Include evaluation is tolerant: an error skips the current pattern and continues.
/// </summary>
public static class GlobMatcher
{
	private static readonly RegexOptions DefaultRegexOptions =
		RegexOptions.IgnoreCase |
		RegexOptions.CultureInvariant |
		RegexOptions.Compiled;

	// A timeout reduces the risk of pathological regex execution consuming too much CPU.
	private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(200);

	// Use a case-insensitive comparer because matching itself is case-insensitive.
	// Cache key format: "{normalizedPattern}\0{(int)mode}"
	private static readonly ConcurrentDictionary<string, Regex> RegexCache = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Returns <c>true</c> when <paramref name="path"/> should be processed
	/// according to include and exclude patterns.
	///
	/// Rules:
	/// 1. If any exclude pattern matches, the path is excluded.
	/// 2. If an exclude pattern errors or times out, evaluation fails fast.
	/// 3. If include patterns are provided, at least one non-blank include pattern must match.
	/// 4. If an include pattern errors or times out, evaluation fails fast.
	/// 5. Blank include patterns are ignored.
	/// 6. If no non-blank include patterns exist, the path is included by default.
	/// </summary>
	public static bool IsIncluded(string path, IEnumerable<string>? includePatterns, IEnumerable<string>? excludePatterns, GlobSeparatorMode separatorMode = GlobSeparatorMode.ForwardSlash)
	{
		ArgumentNullException.ThrowIfNull(path);
		ValidateSeparatorMode(separatorMode);

		if(MatchesAnyExcludePattern(path, excludePatterns, separatorMode, matchOnError: null))
		{
			return false;
		}

		return MatchesIncludePatterns(path, includePatterns, separatorMode, matchOnError: null);
	}

	/// <summary>
	/// Returns <c>true</c> when <paramref name="path"/> matches <paramref name="pattern"/>.
	///
	/// If regex evaluation times out or regex construction fails, the method returns <c>false</c>.
	/// </summary>
	public static bool Matches(string path, string pattern, GlobSeparatorMode separatorMode = GlobSeparatorMode.ForwardSlash)
	{
		ArgumentNullException.ThrowIfNull(path);
		ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
		ValidateSeparatorMode(separatorMode);

		return MatchPath(path, pattern, separatorMode) == MatchResult.Match;
	}

	/// <summary>
	/// Converts a glob pattern into an anchored regex pattern string.
	/// </summary>
	public static string GlobToRegex(string pattern, GlobSeparatorMode separatorMode = GlobSeparatorMode.ForwardSlash)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
		ValidateSeparatorMode(separatorMode);

		string normalizedPattern = NormalizeSeparatorsForBothMode(pattern, separatorMode);
		string glob = ExpandSimpleBraces(normalizedPattern);

		if(TryBuildLeadingRecursiveRegex(glob, out string? regex, separatorMode))
		{
			return regex!;
		}

		if(TryBuildGlobalNegationRegex(glob, out regex, separatorMode))
		{
			return regex!;
		}

		if(TryBuildSuffixNegationRegex(glob, out regex, separatorMode))
		{
			return regex!;
		}

		return ConvertCore(glob, anchor: true, separatorMode);
	}

	private static string SeparatorPattern(GlobSeparatorMode mode) => mode switch
	{
		GlobSeparatorMode.ForwardSlash => "/",
		GlobSeparatorMode.Backslash => @"\\",
		GlobSeparatorMode.Both => @"[/\\]+",
		_ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported separator mode.")
	};

	private static string NonSeparatorClass(GlobSeparatorMode mode) => mode switch
	{
		GlobSeparatorMode.ForwardSlash => @"[^/]",
		GlobSeparatorMode.Backslash => @"[^\\]",
		GlobSeparatorMode.Both => @"[^/\\]",
		_ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported separator mode.")
	};

	private static void ValidateSeparatorMode(GlobSeparatorMode mode)
	{
		if(!Enum.IsDefined(typeof(GlobSeparatorMode), mode))
		{
			throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported separator mode.");
		}
	}

	private static string NormalizeSeparatorsForBothMode(string value, GlobSeparatorMode mode)
	{
		return mode == GlobSeparatorMode.Both
			? value.Replace('\\', '/')
			: value;
	}


	/// <summary>
	/// Returns <c>true</c> if <paramref name="path"/> matches any non-blank exclude pattern.
	/// </summary>
	/// <remarks>
	/// Error handling is controlled by <paramref name="matchOnError"/>:
	/// <c>true</c> = fail-closed, treat errors as matches;
	/// <c>false</c> = fail-open, treat errors as non-matches;
	/// <c>null</c> = fail-fast, throw on errors.
	/// </remarks>
	/// <param name="path">The path to test.</param>
	/// <param name="patterns">The exclude patterns to evaluate. <c>null</c> means no exclude patterns.</param>
	/// <param name="mode">The separator mode used when matching patterns.</param>
	/// <param name="matchOnError">Controls how pattern errors are handled.</param>
	/// <returns><c>true</c> if any exclude pattern matches; otherwise <c>false</c>.</returns>
	/// <exception cref="ArgumentException">
	/// Thrown when a pattern fails and <paramref name="matchOnError"/> is <c>null</c>.
	/// </exception>
	private static bool MatchesAnyExcludePattern(string path, IEnumerable<string>? patterns, GlobSeparatorMode mode = GlobSeparatorMode.ForwardSlash, bool? matchOnError = null)
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

			MatchResult result = MatchPath(path, pattern, mode);

			if(result == MatchResult.Error)
			{
				if(matchOnError == null)
				{
					throw new ArgumentException($"Failed exclude pattern '{pattern}'.", nameof(patterns));
				}

				return matchOnError.Value;
			}

			if(result == MatchResult.Match)
			{
				return true;
			}
		}

		return false;
	}


	private static bool MatchesIncludePatterns(string path, IEnumerable<string>? patterns, GlobSeparatorMode mode = GlobSeparatorMode.ForwardSlash, bool? matchOnError = null)
	{
		if(patterns is null)
		{
			return true;
		}

		bool hasNonBlankIncludePattern = false;

		foreach(string pattern in patterns)
		{
			if(string.IsNullOrWhiteSpace(pattern))
			{
				continue;
			}

			hasNonBlankIncludePattern = true;

			MatchResult result = MatchPath(path, pattern, mode);

			if(result == MatchResult.Error)
			{
				if(matchOnError is null)
				{
					throw new ArgumentException($"Failed include pattern '{pattern}'.", nameof(patterns));
				}

				if(matchOnError.Value)
				{
					return true;
				}

				continue;
			}

			if(result == MatchResult.Match)
			{
				return true;
			}
		}

		return !hasNonBlankIncludePattern;
	}

	private static MatchResult MatchPath(string path, string pattern, GlobSeparatorMode mode)
	{
		try
		{
			string normalizedPath = NormalizeSeparatorsForBothMode(path, mode);
			Regex regex = GetOrCreateRegex(pattern, mode);

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

	private static Regex GetOrCreateRegex(string pattern, GlobSeparatorMode mode)
	{
		string normalizedPattern = NormalizeSeparatorsForBothMode(pattern, mode);
		string cacheKey = $"{normalizedPattern}\0{(int)mode}";

		return RegexCache.GetOrAdd(cacheKey, _ => CreateRegex(normalizedPattern, mode));
	}

	private static Regex CreateRegex(string normalizedPattern, GlobSeparatorMode mode)
	{
		string regexPattern = GlobToRegex(normalizedPattern, mode);
		return new Regex(regexPattern, DefaultRegexOptions, RegexTimeout);
	}

	/// <summary>
	/// Expands simple brace expressions like "{a,b,c}" into "@(a|b|c)".
	///
	/// Empty alternatives are preserved, so "{a,b,}" becomes "@(a|b|)"
	/// and therefore also matches the empty string.
	///
	/// This intentionally supports only flat, comma-separated alternatives.
	/// Nested brace expansion is not supported.
	/// </summary>
	private static string ExpandSimpleBraces(string glob)
	{
		return Regex.Replace(
			glob,
			@"\{([^{}]*,[^{}]*)\}",
			match =>
			{
				string[] alternatives = match.Groups[1].Value
					.Split(',', StringSplitOptions.TrimEntries);

				return "@(" + string.Join("|", alternatives) + ")";
			});
	}

	/// <summary>
	/// Handles the special case where the pattern starts with "**/" or "**\", depending on mode.
	/// This should match both nested paths and files directly at the root.
	/// </summary>
	private static bool TryBuildLeadingRecursiveRegex(string glob, out string? regex, GlobSeparatorMode mode)
	{
		regex = null;

		string? separatorRegex = null;
		int prefixLength = 0;

		if(mode != GlobSeparatorMode.Backslash && glob.StartsWith("**/", StringComparison.Ordinal))
		{
			separatorRegex = SeparatorPattern(mode);
			prefixLength = 3;
		} else if(mode != GlobSeparatorMode.ForwardSlash && glob.StartsWith(@"**\", StringComparison.Ordinal))
		{
			separatorRegex = SeparatorPattern(mode);
			prefixLength = 3;
		}

		if(separatorRegex is null)
			return false;

		string remainder = glob.Substring(prefixLength);
		regex = $"^(?:.*{separatorRegex})?" + ConvertCore(remainder, anchor: false, mode) + "$";
		return true;
	}

	/// <summary>
	/// Handles global negation like "!(pattern)".
	/// </summary>
	private static bool TryBuildGlobalNegationRegex(string glob, out string? regex, GlobSeparatorMode mode)
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

		// Inside global negation, '|' is treated as alternation.
		// ConvertCore escapes it as '\|', so restore it here.
		string positiveRegex = ConvertCore(positivePattern, anchor: false, mode)
			.Replace(@"\|", "|");

		regex = $"^(?!(?:{positiveRegex})$).*$";
		return true;
	}

	/// <summary>
	/// Handles suffix negation like "*.!(jpg)" or "*.!(jpg|png)".
	/// </summary>
	private static bool TryBuildSuffixNegationRegex(string glob, out string? regex, GlobSeparatorMode mode)
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

		string prefixRegex = ConvertCore(prefixGlob, anchor: false, mode);

		// Inside suffix negation, '|' is treated as alternation.
		// ConvertCore escapes it as '\|', so restore it here.
		string negatedSuffixRegex = ConvertCore(negatedSuffixGlob, anchor: false, mode)
			.Replace(@"\|", "|");

		string nonSeparator = NonSeparatorClass(mode);

		regex = $"^(?>{prefixRegex})(?!(?:{negatedSuffixRegex})$){nonSeparator}*$";
		return true;
	}

	/// <summary>
	/// Converts the core glob syntax into regex syntax.
	/// </summary>
	private static string ConvertCore(string glob, bool anchor, GlobSeparatorMode mode)
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

		string separatorRegex = SeparatorPattern(mode);
		string nonSeparator = NonSeparatorClass(mode);

		// Mark recursive directory wildcards before separator conversion.
		// This preserves the "zero or more directory segments" semantics for patterns like "a/**/b.txt".
		const string recursiveDirectoryPlaceholder = "\uE000";

		if(mode != GlobSeparatorMode.Backslash)
			regex = regex.Replace(@"\*\*/", recursiveDirectoryPlaceholder);

		if(mode != GlobSeparatorMode.ForwardSlash)
			regex = regex.Replace(@"\*\*\\", recursiveDirectoryPlaceholder);

		// Convert literal path separators into a separator-aware and tolerant regex.
		// Use a placeholder to avoid corruption: in Both mode, separatorRegex is [/\\]+
		// which contains both / and \, so replacing one separator can otherwise affect the next replacement.
		const string separatorPlaceholder = "\uE001";

		if(mode != GlobSeparatorMode.ForwardSlash)
			regex = Regex.Replace(regex, @"\\{2}", separatorPlaceholder);

		if(mode != GlobSeparatorMode.Backslash)
			regex = regex.Replace("/", separatorPlaceholder);

		regex = regex.Replace(separatorPlaceholder, separatorRegex);

		// Convert recursive directory matching.
		string recursiveDirectoryMatcher = $@"(?:{nonSeparator}*{separatorRegex})*";
		regex = regex.Replace(recursiveDirectoryPlaceholder, recursiveDirectoryMatcher);

		// Convert remaining recursive wildcards.
		regex = regex.Replace(@"\*\*", ".*");

		// Convert standard wildcards.
		regex = regex.Replace(@"\*", $@"{nonSeparator}*");

		// '?' means exactly one non-separator character.
		regex = regex.Replace(@"\?", nonSeparator);

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

