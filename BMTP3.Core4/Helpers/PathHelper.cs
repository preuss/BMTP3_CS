using BMTP3.Core4.Api.Models.Enums;

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
	public const char CanonicalSeparator = '/';
	public const char FileSystemSeparator = '\\';

	private const string MediaDeviceUriScheme = "mtp://";
	private const string FileUriScheme = "file://";
	private const string LocalFileUriPrefix = "file:///";

	public static string ToInternalCanonicalUri(string externalSourcePath, BackupSourceType sourceType)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(externalSourcePath);

		string trimmedExternalSourcePath = externalSourcePath.Trim();

		BackupSourceType detectedSourceType = DetectSourceType(trimmedExternalSourcePath);

		if (detectedSourceType != sourceType)
			throw new ArgumentException($"Source type mismatch. Expected '{sourceType}', but input looks like '{detectedSourceType}'.", nameof(sourceType));

		string internalCanonicalUri = sourceType switch
		{
			BackupSourceType.FileSystem => NormalizeToCanonicalFileUri(trimmedExternalSourcePath),
			BackupSourceType.MediaDevice => NormalizeSeparators(trimmedExternalSourcePath),
			_ => throw new ArgumentOutOfRangeException(nameof(sourceType), $"Unsupported source type: {sourceType}")
		};

		// Redundant check to ensure the internal canonical URI matches the expected source type.
		// I like it, therefore it stays. It ensures that the internal representation is consistent with the expected source type.
		detectedSourceType = DetectSourceType(internalCanonicalUri);
		if(detectedSourceType != sourceType)
			throw new ArgumentException($"Source type mismatch. Expected '{sourceType}', but internal canonical URI looks like '{detectedSourceType}'.", nameof(sourceType));

		return internalCanonicalUri;
	}
	private static BackupSourceType DetectSourceType(string source)
	{
		if(source.StartsWith(FileUriScheme, StringComparison.OrdinalIgnoreCase))
			return BackupSourceType.FileSystem;

		if(source.StartsWith(MediaDeviceUriScheme, StringComparison.OrdinalIgnoreCase))
			return BackupSourceType.MediaDevice;

		if(IsWindowsAbsolutePath(source))
			return BackupSourceType.FileSystem;

		if (IsWindowsUncPath(source))
			return BackupSourceType.FileSystem;

		throw new ArgumentException($"Unsupported source path format: '{source}'", nameof(source));
	}

	private static bool IsWindowsAbsolutePath(string path)
	{
		if(path.Length < 3)
			return false;

		return
			char.IsLetter(path[0]) &&
			path[1] == ':' &&
			(path[2] == '\\' || path[2] == '/');
	}

	private static bool IsWindowsUncPath(string path)
	{
		if(!path.StartsWith(@"\\", StringComparison.Ordinal))
			return false;

		string normalized = NormalizeSeparators(path);

		string[] parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);

		return parts.Length >= 2;
	}
	
	/// <summary>
	/// Converts an absolute file system path to a canonical file URI.
	/// </summary>
	/// <param name="externalSourcePath">
	/// The external file system path to convert. Must be an absolute path
	/// using the operating system's native separator (e.g. <c>'\'</c> on Windows).
	/// </param>
	/// <returns>
	/// A canonical file URI string where:
	/// <list type="bullet">
	///   <item><description>The scheme is <c>file://</c></description></item>
	///   <item><description>All path separators are normalized to <see cref="PathHelper.CanonicalSeparator"/></description></item>
	/// </list>
	/// 
	/// Example:
	/// <code>
	/// C:\source\file.jpg → file:///c:/source/file.jpg
	/// </code>
	/// </returns>
	/// <exception cref="ArgumentNullException">
	/// Thrown when <paramref name="externalSourcePath"/> is <c>null</c>.
	/// </exception>
	/// <exception cref="ArgumentException">
	/// Thrown when <paramref name="externalSourcePath"/> is not an absolute path.
	/// </exception>
	/// <remarks>
	/// This method performs the following steps:
	/// <list type="number">
	///   <item><description>Validates that the input is non-null and absolute</description></item>
	///   <item><description>Resolves the full path using <see cref="Path.GetFullPath(string)"/></description></item>
	///   <item><description>Normalizes separators to the canonical separator (<c>'/'</c>)</description></item>
	///   <item><description>Prefixes the path with the <c>file:///</c> scheme</description></item>
	/// </list>
	/// 
	/// The returned value is intended for internal use within the system's canonical
	/// path model and is not URI-encoded.
	/// </remarks>
	private static string NormalizeToCanonicalFileUri(string externalSourcePath)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(externalSourcePath);

		string trimmed = externalSourcePath.Trim();

		string uriPrefix;
		string pathPart;
		bool isShare;
		bool isAlreadyFileUri;

		if(trimmed.StartsWith(LocalFileUriPrefix, StringComparison.OrdinalIgnoreCase))
		{
			uriPrefix = LocalFileUriPrefix;
			pathPart = trimmed.Substring(LocalFileUriPrefix.Length);
			isShare = false;
			isAlreadyFileUri = true;

			if(!IsWindowsAbsolutePath(pathPart))
				throw new ArgumentException("File URI must contain an absolute Windows path.", nameof(externalSourcePath));
		} else if(trimmed.StartsWith(FileUriScheme, StringComparison.OrdinalIgnoreCase))
		{
			uriPrefix = FileUriScheme;
			pathPart = trimmed.Substring(FileUriScheme.Length);
			isShare = true;
			isAlreadyFileUri = true;

			if(!IsValidSharePathPart(pathPart))
				throw new ArgumentException("File share URI must contain server and share name.", nameof(externalSourcePath));
		} else if(IsWindowsAbsolutePath(trimmed))
		{
			uriPrefix = LocalFileUriPrefix;
			pathPart = trimmed;
			isShare = false;
			isAlreadyFileUri = false;
		} else if(IsWindowsUncPath(trimmed))
		{
			uriPrefix = FileUriScheme;
			pathPart = trimmed;
			isShare = true;
			isAlreadyFileUri = false;
		} else
		{
			throw new ArgumentException("Path must be an absolute file system path or file URI.", nameof(externalSourcePath));
		}

		string canonicalPath;

		if(isShare)
		{
			canonicalPath = isAlreadyFileUri
				? pathPart
				: Path.GetFullPath(pathPart);
			canonicalPath = NormalizeSeparators(canonicalPath);
			canonicalPath = TrimSeparators(canonicalPath, CanonicalSeparator);

			if(!IsValidSharePathPart(canonicalPath))
				throw new ArgumentException("File share path must contain server and share name.", nameof(externalSourcePath));
		} else
		{
			canonicalPath = Path.GetFullPath(pathPart);
			canonicalPath = NormalizeWindowsDriveLetter(canonicalPath);
			canonicalPath = NormalizeSeparators(canonicalPath);
		}

		return $"{uriPrefix}{canonicalPath}";
	}

	private static bool IsValidSharePathPart(string pathPart)
	{
		string normalized = NormalizeSeparators(pathPart).Trim(CanonicalSeparator);

		string[] parts = normalized.Split(
			CanonicalSeparator,
			StringSplitOptions.RemoveEmptyEntries);

		return parts.Length >= 2;
	}

	private static string NormalizeWindowsDriveLetter(string path)
	{
		if(path.Length >= 2 && char.IsLetter(path[0]) && path[1] == ':')
			return char.ToLowerInvariant(path[0]) + path.Substring(1);

		return path;
	}

	public static string FromCanonicalFileUri(string canonicalUri)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(canonicalUri);

		const string prefix = "file:///";

		if(!canonicalUri.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
		{
			throw new ArgumentException($"Invalid file URI: '{canonicalUri}'", nameof(canonicalUri));
		}

		string path = canonicalUri.Substring(prefix.Length);

		return NormalizeSeparators(path, FileSystemSeparator);
	}

	/// <summary>
	/// Extracts the sub-drive path from a canonical URI by stripping the scheme, authority,
	/// and the first path segment (drive letter for <c>file:///</c>, drive name for <c>mtp://</c>).
	/// Returns empty string when the URI points to the drive root with no sub-path.
	/// </summary>
	/// <param name="canonicalUri">A canonical URI (<c>file:///</c> or <c>mtp://</c>).</param>
	/// <exception cref="ArgumentException">
	/// Thrown when <paramref name="canonicalUri"/> is not a valid canonical URI.
	/// </exception>
	/// <example>
	/// <code>
	/// FromCanonicalUriSubDrivePath("file:///c:/DCIM/Camera")    → "DCIM/Camera"
	/// FromCanonicalUriSubDrivePath("mtp://Phone/Storage/DCIM")   → "DCIM"
	/// FromCanonicalUriSubDrivePath("file:///c:/")                → ""
	/// FromCanonicalUriSubDrivePath("mtp://Phone/Storage")        → ""
	/// </code>
	/// </example>
	public static string FromCanonicalUriSubDrivePath(string canonicalUri)
	{
		ArgumentNullException.ThrowIfNull(canonicalUri);

		int schemeEnd = canonicalUri.IndexOf("://", StringComparison.Ordinal);
		if(schemeEnd < 0)
			throw new ArgumentException($"Not a canonical URI: '{canonicalUri}'", nameof(canonicalUri));

		int afterScheme = schemeEnd + 3;
		int pathStart = canonicalUri.IndexOf('/', afterScheme);
		if(pathStart < 0)
			throw new ArgumentException($"Cannot extract sub-drive path from: '{canonicalUri}'", nameof(canonicalUri));

		string path = canonicalUri.Substring(pathStart + 1);
		int firstSegmentEnd = path.IndexOf('/');

		return firstSegmentEnd < 0
			? ""
			: path.Substring(firstSegmentEnd + 1).TrimStart('/');
	}

	/// <summary>
	/// Joins path segments using the canonical separator (<c>'/'</c>).
	/// Empty or null segments are silently skipped.
	/// </summary>
	public static string JoinPathSegments(params string[] segments)
	{
		return string.Join("/", segments.Where(s => !string.IsNullOrEmpty(s)));
	}

	/// <summary>
	/// Splits a path on the canonical separator (<c>'/'</c>).
	/// Empty segments are excluded from the result.
	/// </summary>
	public static IEnumerable<string> SplitPath(string path)
	{
		ArgumentNullException.ThrowIfNull(path);

		foreach(string segment in path.Split(CanonicalSeparator))
		{
			if(segment.Length > 0)
				yield return segment;
		}
	}

	/// <summary>
	/// Normalizes path separators in a single value to the specified separator.
	/// </summary>
	/// <param name="path">The value to normalize.</param>
	/// <param name="separator">
	/// The separator to normalize to.
	/// Must be either <see cref="CanonicalSeparator"/> (<c>'/'</c>, canonical/URI)
	/// or <see cref="FileSystemSeparator"/> (<c>'\'</c>, file system).
	/// </param>
	/// <returns>
	/// The input value where all occurrences of both
	/// <see cref="CanonicalSeparator"/> and <see cref="FileSystemSeparator"/>
	/// have been replaced with <paramref name="separator"/>.
	/// </returns>
	/// <exception cref="ArgumentNullException">
	/// Thrown when <paramref name="path"/> is <c>null</c>.
	/// </exception>
	/// <exception cref="ArgumentException">
	/// Thrown when <paramref name="separator"/> is neither
	/// <see cref="CanonicalSeparator"/> nor <see cref="FileSystemSeparator"/>.
	/// </exception>
	/// <remarks>
	/// This method performs a simple separator normalization by replacing both
	/// supported separator types with the requested one.
	/// 
	/// It does not:
	/// <list type="bullet">
	///   <item><description>trim leading or trailing separators</description></item>
	///   <item><description>validate the path structure</description></item>
	///   <item><description>interpret <c>.</c> or <c>..</c> segments</description></item>
	///   <item><description>remove empty path segments</description></item>
	/// </list>
	/// 
	/// The method is deterministic and does not perform any additional normalization
	/// beyond character replacement.
	/// </remarks>
	public static string NormalizeSeparators(string path, char separator = CanonicalSeparator)
	{
		ArgumentNullException.ThrowIfNull(path);
		ThrowIfInvalidSeparator(separator);

		return path
			.Replace(FileSystemSeparator, separator)
			.Replace(CanonicalSeparator, separator);
	}


	/// <summary>
	/// Throws an <see cref="ArgumentException"/> if the specified separator is not supported.
	/// </summary>
	/// <param name="separator">
	/// The separator character to validate.
	/// Must be either <c>'/'</c> (canonical / URI) or <c>'\'</c> (file system).
	/// </param>
	/// <exception cref="ArgumentException">
	/// Thrown when <paramref name="separator"/> is neither the canonical separator
	/// (<see cref="CanonicalSeparator"/>) nor the file system separator
	/// (<see cref="FileSystemSeparator"/>).
	/// </exception>
	/// <remarks>
	/// This method enforces the invariant that only two path separator types are allowed
	/// within the system:
	/// <list type="bullet">
	///   <item>
	///     <description>
	///     <c>'/'</c> — the canonical separator used internally, for URI representations,
	///     and for glob matching.
	///     </description>
	///   </item>
	///   <item>
	///     <description>
	///     <c>'\'</c> — the file system separator used when interacting with Windows paths.
	///     </description>
	///   </item>
	/// </list>
	/// 
	/// The method follows a fail-fast approach and should be used to guard public
	/// or internal APIs that accept a separator argument.
	/// </remarks>
	private static void ThrowIfInvalidSeparator(char separator)
	{
		if(separator is
		   not CanonicalSeparator and
		   not FileSystemSeparator
		)
		{
			throw new ArgumentException($"Separator must be '{CanonicalSeparator}' (canonical/URI) or '{FileSystemSeparator}' (file system).", nameof(separator));
		}
	}

	/// <summary>
	/// Create a relative path from one path to another. Paths will be resolved before calculating the difference.
	/// Comparison is ordinal case-insensitive (<see cref="StringComparison.OrdinalIgnoreCase"/>).
	/// </summary>
	/// <param name="rootDirectory">The source path the output should be relative to. This path is always considered to be a directory.</param>
	/// <param name="sourcePath">The destination path.</param>
	/// <returns>The relative path or <paramref name="sourcePath"/> if the paths don't share the same root.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="rootDirectory"/> or <paramref name="sourcePath"/> is <c>null</c> or an empty string.</exception>
	public static string GetRelativePath(string rootDirectory, string sourcePath)
	{
		ArgumentNullException.ThrowIfNull(rootDirectory);
		ArgumentNullException.ThrowIfNull(sourcePath);

		if(string.Equals(rootDirectory, sourcePath, StringComparison.OrdinalIgnoreCase))
			return string.Empty;

		if(!sourcePath.StartsWith(rootDirectory, StringComparison.OrdinalIgnoreCase))
			throw new ArgumentException($"Path '{sourcePath}' is not relative to '{rootDirectory}'.");

		return sourcePath
			.Substring(rootDirectory.Length)
			.TrimStart(CanonicalSeparator);
	}

	// Windows device names that must not be used as a file or directory name.
	// The check applies to the stem (everything before the first dot), so both
	// "CON" and "NUL.txt" are caught.
	private static readonly string[] ReservedDeviceNames =
	{
		"CON", "PRN", "AUX", "NUL",
		"COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
		"LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
	};

	/// <summary>
	/// Turns a single file or directory name into a name that is valid on Windows.
	/// </summary>
	/// <remarks>
	/// The transformation is <b>deterministic</b> (no time, culture or file system state),
	/// <b>total for non-null input</b> (never throws a formatting error) and <b>idempotent</b>
	/// (<c>f(f(x)) == f(x)</c>), so it is safe to call for every backup run and on resume.
	///
	/// Steps:
	/// <list type="number">
	///   <item><description>
	///   Every reserved character (<c>0x00</c>-<c>0x1F</c> and <c>\ / : * ? " &lt; &gt; |</c>)
	///   is replaced with <c>_</c>.
	///   </description></item>
	///   <item><description>
	///   Trailing spaces and dots are trimmed. Leading characters are preserved, because
	///   Windows allows them (e.g. <c>.gitignore</c>).
	///   </description></item>
	///   <item><description>
	///   An empty result becomes <c>_</c>.
	///   </description></item>
	///   <item><description>
	///   A reserved device name gets an underscore inserted after its stem:
	///   <c>CON</c> → <c>CON_</c>, <c>NUL.txt</c> → <c>NUL_.txt</c>.
	///   </description></item>
	/// </list>
	///
	/// The mapping is lossy on purpose: only characters that Windows refuses are touched,
	/// so legal characters such as <c>@</c> or <c>æøå</c> pass through unchanged. The
	/// original name stays available in the sidecar and session state.
	/// </remarks>
	/// <param name="name">The raw name. Must not be <c>null</c>.</param>
	/// <returns>A Windows-safe equivalent of <paramref name="name"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is <c>null</c>.</exception>
	public static string SanitizeFileNameComponent(string name)
	{
		ArgumentNullException.ThrowIfNull(name);

		if(name.Length == 0)
			return "_";

		char[] sanitized = name.ToCharArray();

		for(int i = 0; i < sanitized.Length; i++)
		{
			if(sanitized[i] < ' ' || sanitized[i] is '\\' or '/' or ':' or '*' or '?' or '"' or '<' or '>' or '|')
				sanitized[i] = '_';
		}

		string result = new string(sanitized).TrimEnd(' ', '.');

		if(result.Length == 0)
			return "_";

		int stemEnd = result.IndexOf('.');

		if(stemEnd < 0)
		{
			stemEnd = result.Length;
		}

		string stem = result.Substring(0, stemEnd);

		if(IsReservedDeviceName(stem))
		{
			return stem + "_" + result.Substring(stemEnd);
		}

		return result;
	}

	/// <summary>
	/// Sanitizes every component of a relative path so that the resulting path is valid on Windows.
	/// </summary>
	/// <remarks>
	/// The path is split on both <c>\</c> and <c>/</c> before each component is passed through
	/// <see cref="SanitizeFileNameComponent"/>. Only the components are rewritten: the separators
	/// and the overall structure (including empty components) are preserved, so this method never
	/// normalizes. Use <see cref="NormalizeCustomRelativePath"/> for separator and segment
	/// normalization.
	/// A <c>.</c> or <c>..</c> component collapses to <c>_</c> because the component sanitizer
	/// trims trailing dots, so a leaked traversal component cannot escape the destination root.
	/// </remarks>
	/// <param name="relativePath">The raw relative path. Must not be <c>null</c>.</param>
	/// <returns>A Windows-safe relative path that keeps the input separators.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="relativePath"/> is <c>null</c>.</exception>
	public static string SanitizeRelativePath(string relativePath)
	{
		ArgumentNullException.ThrowIfNull(relativePath);

		List<string> pieces = new();

		int segmentStart = 0;

		for(int i = 0; i <= relativePath.Length; i++)
		{
			bool atEnd = i == relativePath.Length;

			if(!atEnd)
			{
				char current = relativePath[i];

				if(current != FileSystemSeparator && current != CanonicalSeparator)
				{
					continue;
				}
			}

			if(i > segmentStart)
			{
				pieces.Add(SanitizeFileNameComponent(relativePath.Substring(segmentStart, i - segmentStart)));
			}

			if(!atEnd)
			{
				pieces.Add(relativePath[i].ToString());
			}

			segmentStart = i + 1;
		}

		return string.Concat(pieces);
	}

	/// <summary>
	/// Returns whether <paramref name="stem"/> is a reserved Windows device name.
	/// Comparison is case-insensitive, matching how Windows resolves these names.
	/// </summary>
	private static bool IsReservedDeviceName(string stem)
	{
		return Array.Exists(
			ReservedDeviceNames,
			reserved => string.Equals(stem, reserved, StringComparison.OrdinalIgnoreCase));
	}

	#region LegacyPathHelpers (to be removed after canonical refactor)

	/// <summary>
	/// ⚠️ Legacy method.
	/// Used by pre-canonical path pipeline.
	/// Will be removed once traversal and scanning is migrated to canonical URI model.
	/// </summary>
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
		if(normalizeNull && string.IsNullOrWhiteSpace(relativePath))
		{
			return string.Empty;
		}

		ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

		char separator = Path.DirectorySeparatorChar;

		// Normalize separators.
		// This replaces both '/' and '\' with the internal separator.
		// Both files "/Folder/File.jpg" and "\Folder\File.jpg" 
		// normalize to "\Folder\File.jpg".
		// Both folders "/Folder/SubFolder/" and "\Folder\SubFolder\"
		// normalize to "\Folder\SubFolder\".
		string normalized = relativePath
			.Trim()
			.Replace('\\', separator)
			.Replace('/', separator);

		// ':' is not valid inside this custom relative path format.
		// This also rejects Windows drive prefixes such as "C:\Temp\File.jpg"
		// and full custom URI strings such as "mtp://Device/Storage/File.jpg".
		if(normalized.Contains(':'))
		{
			throw new ArgumentException("Relative path must not contain ':'.", nameof(relativePath));
		}

		// Leading and trailing separators are treated as input noise.
		// Removing them ensures that the normalized path is a clean relative path.
		// File path like "\Folder\File.jpg" is trimmed to "Folder\File.jpg".
		// Folder path like "\Folder\OtherFolder\" is trimmed to "Folder\OtherFolder".
		normalized = TrimSeparators(normalized, separator);

		if(normalized.Length == 0)
		{
			if(normalizeNull)
			{
				return string.Empty;
			}

			throw new ArgumentException("Relative path must contain at least one non-separator segment.", nameof(relativePath));
		}

		string[] parts = normalized.Split(separator, StringSplitOptions.RemoveEmptyEntries);

		foreach(string part in parts)
		{
			if(part is "." or "..")
			{
				throw new ArgumentException("Relative path must not contain current or parent directory traversal.", nameof(relativePath));
			}
		}

		return string.Join(separator, parts);
	}

	/// <summary>
	/// Removes leading and trailing occurrences of the specified separator.
	/// </summary>
	/// <param name="path">The value to trim.</param>
	/// <param name="separator">
	/// The separator to trim. Must be either <c>/</c> or <c>\</c>.
	/// </param>
	/// <returns>
	/// The input value without leading or trailing occurrences of <paramref name="separator"/>.
	/// </returns>
	/// <exception cref="ArgumentNullException">
	/// Thrown when <paramref name="path"/> is <c>null</c>.
	/// </exception>
	/// <exception cref="ArgumentException">
	/// Thrown when <paramref name="separator"/> is not <c>/</c> or <c>\</c>.
	/// </exception>
	/// <remarks>
	/// This method only trims the specified separator.
	/// It does not normalize separators first.
	/// </remarks>
	public static string TrimSeparators(string path, char separator)
	{
		ArgumentNullException.ThrowIfNull(path);
		ThrowIfInvalidSeparator(separator);

		return path.Trim(separator);
	}

	/// <summary>
	/// ⚠️ Legacy method.
	/// Used by pre-canonical path pipeline.
	/// Will be removed once traversal and scanning is migrated to canonical URI model.
	/// </summary>
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
	/// ⚠️ Legacy method.
	/// Used by pre-canonical path pipeline.
	/// Will be removed once traversal and scanning is migrated to canonical URI model.
	/// </summary>
	/// <summary>
	/// Normalizes each glob pattern to use <c>/</c> as separator.
	/// Accepts both <c>/</c> and <c>\</c> as input separators.
	/// Returns <c>null</c> when <paramref name="patterns"/> is <c>null</c>,
	/// the same instance when empty, and a new array otherwise.
	/// </summary>
	public static IReadOnlyList<string>? NormalizePatterns(IReadOnlyList<string>? patterns)
	{
		if(patterns is null)
		{
			return null;
		}

		if(patterns.Count == 0)
		{
			return patterns;
		}

		string[] result = new string[patterns.Count];

		for(int i = 0; i < patterns.Count; i++)
		{
			result[i] = patterns[i].Replace('\\', '/').Trim('/');
		}

		return result;
	}
	#endregion
}