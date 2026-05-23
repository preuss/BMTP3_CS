using BMTP3.Core4.Engine.Session;
using System.Text;

namespace BMTP3.Core4.Engine;

/// <summary>
/// Provides helper methods for creating and managing temporary directories and files
/// used during a backup session.
/// 
/// Design goals:
/// - Fully traceable file names for debugging
/// - Safe for Windows file systems
/// - Clear separation between structure and implementation
/// </summary>
internal static class TempDirectoryHelper
{
	private const string TempRootDirName = ".tmp";
	private const string TempFileExtension = ".tmp";
	private const string TimestampFormat = "yyyyMMdd_HHmmss";
	private const string ElementSeparator = "_";
	private const int MaxFileNameLength = 200;

	private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

	private static readonly string[] ReservedDeviceNames =
	{
		"CON","PRN","AUX","NUL",
		"COM1","COM2","COM3","COM4","COM5","COM6","COM7","COM8","COM9",
		"LPT1","LPT2","LPT3","LPT4","LPT5","LPT6","LPT7","LPT8","LPT9"
	};

	// =========================
	// DIRECTORY
	// =========================

	/// <summary>
	/// Builds the temporary directory path for a backup session.
	/// Format: {destination}\.tmp\{timestamp}_{sessionId}
	/// </summary>
	public static DirectoryInfo ResolveTempDirectoryPath(
		string destination,
		DateTimeOffset backupStartTime,
		BackupSessionKey sessionKey)
	{
		ArgumentNullException.ThrowIfNull(destination);
		ArgumentNullException.ThrowIfNull(sessionKey);

		string rootPart = BuildTempRootPart(destination);
		string timestampPart = BuildTimestampPart(backupStartTime);
		string sessionPart = BuildSessionPart(sessionKey);
		string separatorPart = ElementSeparator;

		string directoryName = $"{timestampPart}{separatorPart}{sessionPart}";
		string fullPath = Path.Combine(rootPart, directoryName);

		return new DirectoryInfo(fullPath);
	}

	/// <summary>
	/// Builds the root temporary folder path (destination\.tmp).
	/// </summary>
	private static string BuildTempRootPart(string destination)
	{
		return Path.Combine(destination, TempRootDirName);
	}

	/// <summary>
	/// Formats the backup start time as a timestamp string.
	/// </summary>
	private static string BuildTimestampPart(DateTimeOffset time)
	{
		return time.ToString(TimestampFormat);
	}

	/// <summary>
	/// Returns the session identifier used in the temp directory name.
	/// </summary>
	private static string BuildSessionPart(BackupSessionKey sessionKey)
	{
		return sessionKey.SessionId;
	}

	// =========================
	// DIRECTORY PREP
	// =========================

	/// <summary>
	/// Ensures that the temporary directory exists.
	/// Creates it if it does not already exist.
	/// </summary>
	public static void PrepareTempDirectory(DirectoryInfo tempDir)
	{
		ArgumentNullException.ThrowIfNull(tempDir);

		if(tempDir.Exists)
			return;

		tempDir.Create();
		tempDir.Refresh();
	}

	// =========================
	// FILE NAME (GUID + BASENAME)
	// =========================

	/// <summary>
	/// Builds a temporary file name using the pattern:
	/// {guid}_{sanitizedFileName}.tmp
	/// 
	/// Guarantees uniqueness via GUID and preserves the original file name (sanitized)
	/// for traceability. If necessary, the file name is truncated to fit within
	/// the maximum allowed length.
	/// </summary>
	public static string BuildTempFileName(string fileName)
	{
		ArgumentNullException.ThrowIfNull(fileName);

		string guidPart = BuildGuidPart();
		string basePart = BuildBasePart(fileName);
		string separatorPart = ElementSeparator;
		string extensionPart = TempFileExtension;

		string adjustedBasePart = EnforceLength(
			guidPart,
			separatorPart,
			basePart,
			extensionPart);

		string tempFileName =
			$"{guidPart}{separatorPart}{adjustedBasePart}{extensionPart}";

		return tempFileName;
	}

	/// <summary>
	/// Builds the full temporary file path within the given temp directory.
	/// Calls <see cref="BuildTempFileName"/> and combines it with <paramref name="tempDir"/>.
	/// </summary>
	public static string BuildTempFilePath(DirectoryInfo tempDir, string fileName)
	{
		ArgumentNullException.ThrowIfNull(tempDir);

		return Path.Combine(tempDir.FullName, BuildTempFileName(fileName));
	}

	// =========================
	// FILE NAME PARTS
	// =========================

	/// <summary>
	/// Generates a unique GUID string without separators.
	/// </summary>
	private static string BuildGuidPart()
	{
		return Guid.NewGuid().ToString("N");
	}

	/// <summary>
	/// Sanitizes the original file name:
	/// - Removes invalid characters
	/// - Normalizes Unicode
	/// - Collapses repeated underscores
	/// - Trims trailing spaces and dots
	/// - Handles reserved Windows device names
	/// </summary>
	private static string BuildBasePart(string fileName)
	{
		string normalized = fileName.Normalize(NormalizationForm.FormC);

		StringBuilder sb = new StringBuilder();

		for(int i = 0; i < normalized.Length; i++)
		{
			char c = normalized[i];

			if(InvalidFileNameChars.Contains(c) || char.IsControl(c))
				sb.Append('_');
			else
				sb.Append(c);
		}

		string result = sb.ToString().Trim();

		while(result.Contains("__", StringComparison.Ordinal))
		{
			result = result.Replace("__", "_", StringComparison.Ordinal);
		}

		result = result.TrimEnd(' ', '.');

		// Reserved device names
		for(int i = 0; i < ReservedDeviceNames.Length; i++)
		{
			if(string.Equals(result, ReservedDeviceNames[i], StringComparison.OrdinalIgnoreCase))
			{
				result += "_file";
				break;
			}
		}

		if(string.IsNullOrEmpty(result))
		{
			throw new ArgumentException($"File name '{fileName}' produced an empty filename.");
		}

		return result;
	}

	// =========================
	// LENGTH CONTROL
	// =========================

	/// <summary>
	/// Ensures that the final filename does not exceed the maximum allowed length.
	/// The GUID and extension are preserved, and only the base file name is truncated if needed.
	/// </summary>
	private static string EnforceLength(
		string guidPart,
		string separator,
		string basePart,
		string extension)
	{
		string prefix = $"{guidPart}{separator}";
		string suffix = extension;

		int maxBaseLength = MaxFileNameLength - (prefix.Length + suffix.Length);

		if(maxBaseLength < 1)
			return basePart.Substring(0, 1);

		if(basePart.Length > maxBaseLength)
			return basePart.Substring(0, maxBaseLength);

		return basePart;
	}

	// =========================
	// CLEANUP
	// =========================

	/// <summary>
	/// Attempts to delete temporary files.
	/// Any exceptions are swallowed to avoid interfering with the main workflow.
	/// </summary>
	public static void CleanupTempFiles(string? tempPath, string? tempSidecarPath)
	{
		TryDelete(tempPath);
		TryDelete(tempSidecarPath);
	}

	/// <summary>
	/// Deletes a file if it exists. Failures are ignored.
	/// </summary>
	private static void TryDelete(string? path)
	{
		if(path is null || !File.Exists(path))
			return;

		try
		{
			File.Delete(path);
		} catch
		{
			// intentionally ignored
		}
	}
}