namespace BMTP3.Core4.Helpers;

/// <summary>
/// Path normalization utilities for internal relative paths and glob patterns.
/// </summary>
/// <remarks>
/// <b>Separator convention:</b>
/// <list type="bullet">
///   <item>
///     <description>
///     Internal relative paths use <see cref="Path.DirectorySeparatorChar"/>.
///     </description>
///   </item>
///   <item>
///     <description>
///     Glob patterns use <c>/</c> as required by <c>GlobMatcher</c>.
///     </description>
///   </item>
/// </list>
/// </remarks>
internal static class PathHelper
{
    /// <summary>
    /// Normalizes a custom relative path into the internal path format.
    ///
    /// Rules:
    /// - Both <c>\</c> and <c>/</c> are accepted as input separators.
    /// - Output uses <see cref="Path.DirectorySeparatorChar"/> as the internal separator.
    /// - Leading and trailing separators are treated as input noise and removed.
    /// - Empty path segments are removed.
    /// - <c>:</c> is not allowed anywhere in the relative path.
    /// - Current and parent traversal segments (<c>.</c> and <c>..</c>) are not allowed.
    ///
    /// Examples on Windows:
    /// - <c>"Folder/File.jpg"</c> becomes <c>"Folder\File.jpg"</c>.
    /// - <c>"\Folder\File.jpg"</c> becomes <c>"Folder\File.jpg"</c>.
    /// - <c>"/Folder/File.jpg"</c> becomes <c>"Folder\File.jpg"</c>.
    ///
    /// When <paramref name="normalizeNull"/> is <c>true</c>, <c>null</c> and whitespace-only
    /// input returns <see cref="string.Empty"/> instead of throwing.
    ///
    /// Other invalid input, such as paths containing <c>:</c>, <c>.</c>, or <c>..</c>,
    /// always throws.
    /// </summary>
    public static string NormalizeCustomRelativePath(string? relativePath, bool normalizeNull = false)
    {
        if (normalizeNull && string.IsNullOrWhiteSpace(relativePath))
        {
            return string.Empty;
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        char separator = Path.DirectorySeparatorChar;

        string normalized = relativePath
            .Trim()
            .Replace('\\', separator)
            .Replace('/', separator);

        // ':' is not valid inside this custom relative path format.
        // This also rejects Windows drive prefixes such as "C:\Temp\File.jpg"
        // and full custom URI strings such as "mtp://Device/Storage/File.jpg".
        if (normalized.Contains(':'))
        {
            throw new ArgumentException("Relative path must not contain ':'.", nameof(relativePath));
        }

        // Leading and trailing separators are treated as input noise.
        // This preserves the behavior where "\Folder\File.jpg"
        // and "/Folder/File.jpg" normalize to "Folder\File.jpg".
        normalized = normalized.Trim(separator);

        if (normalized.Length == 0)
        {
            if (normalizeNull)
            {
                return string.Empty;
            }

            throw new ArgumentException("Relative path must contain at least one non-separator segment.", nameof(relativePath));
        }

        string[] parts = normalized.Split(separator, StringSplitOptions.RemoveEmptyEntries);

        foreach (string part in parts)
        {
            if (part is "." or "..")
            {
                throw new ArgumentException("Relative path must not contain current or parent directory traversal.", nameof(relativePath));
            }
        }

        return string.Join(separator, parts);
    }

    /// <summary>
    /// Normalizes a path to use <c>\</c> as separator and removes leading/trailing separators.
    /// Accepts both <c>/</c> and <c>\</c> as input separators.
    /// <para>
    /// <b>WARNING:</b> This is a <b>pure separator normalizer</b>.
    /// Unlike <see cref="NormalizeCustomRelativePath"/>, it does <b>not</b> reject
    /// <c>:</c> drive letters, URIs, traversal segments, or empty paths.
    /// Use <see cref="NormalizeCustomRelativePath"/> when path validation is needed.
    /// </para>
    /// </summary>
    public static string NormalizePath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        return path.Replace('/', '\\').Trim('\\');
    }

    /// <summary>
    /// Normalizes each glob pattern to use <c>/</c> as separator.
    /// Accepts both <c>/</c> and <c>\</c> as input separators.
    /// Returns <c>null</c> when <paramref name="patterns"/> is <c>null</c>,
    /// the same instance when empty, and a new array otherwise.
    /// </summary>
    public static IReadOnlyList<string>? NormalizePatterns(IReadOnlyList<string>? patterns)
    {
        if (patterns is null)
        {
            return null;
        }

        if (patterns.Count == 0)
        {
            return patterns;
        }

        string[] result = new string[patterns.Count];

        for (int i = 0; i < patterns.Count; i++)
        {
            result[i] = patterns[i].Replace('\\', '/').Trim('/');
        }

        return result;
    }
}