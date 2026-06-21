using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace BMTP3.Core4.Helpers;

internal static class PathDisplayCasing
{
	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern uint GetLongPathNameW(string lpszShortPath, StringBuilder? lpszLongPath, int cchBuffer);


	/// <summary>
	/// Resolves an existing file or directory path to the casing reported by the file system.
	/// </summary>
	/// <param name="path">The file or directory path to resolve.</param>
	/// <returns>The absolute path using the file system's display casing.</returns>
	/// <exception cref="ArgumentException">
	/// Thrown when <paramref name="path"/> is null, empty, or whitespace.
	/// </exception>
	/// <exception cref="FileNotFoundException">
	/// Thrown when the path does not exist or its casing cannot be resolved.
	/// </exception>
	/// <exception cref="System.ComponentModel.Win32Exception">
	/// Thrown on Windows if native path resolution fails.
	/// </exception>
	/// <remarks>
	/// Use this for display/output paths. Do not rely on casing for path identity.
	/// </remarks>
	public static string ResolveExistingPathDisplayCasing(string path)
	{
		if(string.IsNullOrWhiteSpace(path))
			throw new ArgumentException("Path must not be null or empty.", nameof(path));

		string fullPath = Path.GetFullPath(path);

		if(!File.Exists(fullPath) && !Directory.Exists(fullPath))
			throw new FileNotFoundException("Path does not exist.", fullPath);

		if(OperatingSystem.IsWindows())
			return GetLongPathName(fullPath);

		return ResolveExistingPathDisplayCasingManaged(fullPath);
	}

	private static string GetLongPathName(string path)
	{
		uint requiredLength = GetLongPathNameW(path, null, 0);

		if(requiredLength == 0)
			throw new Win32Exception(Marshal.GetLastWin32Error());

		StringBuilder buffer = new((int)requiredLength + 1);

		uint actualLength = GetLongPathNameW(path, buffer, buffer.Capacity);

		if(actualLength == 0)
			throw new Win32Exception(Marshal.GetLastWin32Error());

		return buffer.ToString(0, (int)actualLength);
	}

	private static string ResolveExistingPathDisplayCasingManaged(string fullPath)
	{
		string? root = Path.GetPathRoot(fullPath);

		if(string.IsNullOrEmpty(root))
			return fullPath;

		string relative = Path.GetRelativePath(root, fullPath);

		if(relative == ".")
			return root;

		string current = root;

		StringComparison comparison = OperatingSystem.IsWindows()
			? StringComparison.OrdinalIgnoreCase
			: StringComparison.Ordinal;

		foreach(string part in relative.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries))
		{
			DirectoryInfo parent = new(current);

			FileSystemInfo? match = parent.EnumerateFileSystemInfos()
				.FirstOrDefault(x =>
					string.Equals(x.Name, part, comparison)
				);

			if(match is null)
				throw new FileNotFoundException("Could not resolve actual casing.", fullPath);

			current = Path.Combine(current, match.Name);
		}

		return current;
	}
}