namespace BMTP3.Core4.Helpers;

internal static class PathHelper
{
	public static string NormalizeCustomRelativePath(string relativePath)
	{
		if(string.IsNullOrWhiteSpace(relativePath))
		{
			throw new InvalidOperationException("Relative path must not be null or empty.");
		}

		string normalized = relativePath
			.Trim()
			.Replace('\\', Path.DirectorySeparatorChar)
			.Replace('/', Path.DirectorySeparatorChar);

		// Block Windows drive rooted paths like C:\Temp\file.jpg
		if(normalized.Length >= 2 && normalized[1] == ':')
		{
			throw new InvalidOperationException("Relative path must not be an absolute path. Remove drive letter.");
		}

		normalized = normalized.Trim(Path.DirectorySeparatorChar);

		if(normalized.Length == 0)
		{
			throw new InvalidOperationException("Relative path consists only of directory separators.");
		}

		string[] parts = normalized.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

		if(parts.Any(part => part == ".."))
		{
			throw new InvalidOperationException("Relative path must not contain parent directory traversal.");
		}

		return string.Join(Path.DirectorySeparatorChar, parts);
	}
}