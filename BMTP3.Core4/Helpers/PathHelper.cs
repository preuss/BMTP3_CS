namespace BMTP3.Core4.Helpers;

internal static class PathHelper
{
	/// <summary>
	/// Normalizes a custom relative path into the internal path format.
	///
	/// Rules:
	/// - Both '\' and '/' are accepted as input separators.
	/// - Output uses <see cref="Path.DirectorySeparatorChar"/> as the internal separator.
	/// - Leading and trailing separators are treated as input noise and removed.
	/// - Empty path segments are removed.
	/// - ':' is not allowed anywhere in the relative path.
	/// - Parent traversal segments ("..") are not allowed.
	///
	/// Examples:
	/// - "Folder/File.jpg" becomes "Folder\File.jpg" on Windows.
	/// - "\Folder\File.jpg" becomes "Folder\File.jpg" on Windows.
	/// - "/Folder/File.jpg" becomes "Folder\File.jpg" on Windows.
	///
	/// When <paramref name="normalizeNull"/> is <c>true</c>, <c>null</c> and whitespace-only
	/// input returns <see cref="string.Empty"/> instead of throwing.
	///
	/// Other invalid input, such as paths containing ':' or parent traversal segments (".."),
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
		// This preserves the previous behavior where "\Folder\File.jpg"
		// and "/Folder/File.jpg" normalize to "Folder\File.jpg".
		normalized = normalized.Trim(separator);

		if(normalized.Length == 0)
		{
			if(normalizeNull)
				return string.Empty;
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
}