using System;
using System.IO;
using System.Linq;
using System.Text;
using BMTP3.Core4.Engine.Session;

namespace BMTP3.Core4.Engine;

internal static class TempDirectoryHelper
{
	private const string TempRootDirName = ".tmp";
	private const string TempFileExtension = ".tmp";
	private const string TimestampFormat = "yyyyMMdd_HHmmss";
	private const int MaxFileNameLength = 200;
	private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();
	private static readonly string[] ReservedDeviceNames =
	{
		"CON",
		"PRN",
		"AUX",
		"NUL",
		"COM1",
		"COM2",
		"COM3",
		"COM4",
		"LPT1",
		"LPT2",
		"LPT3"
	};

	/// <summary>
	/// Resolve a per-session temp directory under <c>destination\.tmp\{timestamp}_{sessionId}</c>.
	/// </summary>
	public static DirectoryInfo ResolveTempDirectoryPath(string destination, DateTimeOffset backupStartTime, BackupSessionKey sessionKey)
	{
		ArgumentNullException.ThrowIfNull(destination);
		ArgumentNullException.ThrowIfNull(sessionKey);

		string timestamp = backupStartTime.ToString(TimestampFormat);
		string dirName = timestamp + "_" + sessionKey.SessionId;
		return new DirectoryInfo(Path.Combine(destination, TempRootDirName, dirName));
	}

	/// <summary>
	/// Ensure the temp directory exists. Best-effort: rethrow only on unexpected failures.
	/// </summary>
	public static void PrepareTempDirectory(DirectoryInfo tempDir)
	{
		ArgumentNullException.ThrowIfNull(tempDir);

		if (tempDir.Exists)
		{
			return;
		}

		tempDir.Create();
		tempDir.Refresh();
	}

	/// <summary>
	/// Build a temp file name from a source file name: <c>{sanitizedName}_{guid}.tmp</c>.
	/// GUID guarantees uniqueness; the name prefix makes it traceable in <c>.tmp</c>.
	/// Throws <see cref="ArgumentException"/> when the sanitized name would be empty.
	/// </summary>
	public static string BuildTempFileName(string fileName)
	{
		ArgumentNullException.ThrowIfNull(fileName);

		string cleanName = SanitizeFileName(fileName);

		if (string.IsNullOrEmpty(cleanName))
		{
			throw new ArgumentException($"File name '{fileName}' produced an empty filename after sanitization.", nameof(fileName));
		}

		return cleanName + "_" + Guid.NewGuid().ToString("N") + TempFileExtension;
	}

	/// <summary>
	/// Best-effort delete of temp files. Swallows exceptions to avoid masking primary errors.
	/// </summary>
	public static void CleanupTempFiles(string? tempPath, string? tempSidecarPath)
	{
		if (tempPath is not null && File.Exists(tempPath))
		{
			try
			{
				File.Delete(tempPath);
			}
			catch (Exception)
			{
				/* best-effort cleanup - ignore */
			}
		}

		if (tempSidecarPath is not null && File.Exists(tempSidecarPath))
		{
			try
			{
				File.Delete(tempSidecarPath);
			}
			catch (Exception)
			{
				/* best-effort cleanup - ignore */
			}
		}
	}

	/// <summary>
	/// Replace invalid filename characters with underscores, collapse repeated underscores,
	/// trim, enforce max length and avoid reserved device names.
	/// </summary>
	private static string SanitizeFileName(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return string.Empty;
		}

		StringBuilder builder = new StringBuilder(name.Length);

		for (int i = 0; i < name.Length; i = i + 1)
		{
			char c = name[i];

			if (InvalidFileNameChars.Contains(c) || char.IsControl(c))
			{
				builder.Append('_');
			}
			else
			{
				builder.Append(c);
			}
		}

		string candidate = builder.ToString().Trim();

		// Collapse multiple underscores
		while (candidate.Contains("__", StringComparison.Ordinal))
		{
			candidate = candidate.Replace("__", "_", StringComparison.Ordinal);
		}

		// Trim to max allowed length
		if (candidate.Length > MaxFileNameLength)
		{
			candidate = candidate.Substring(0, MaxFileNameLength);
		}

		// Avoid reserved device names (case-insensitive)
		for (int i = 0; i < ReservedDeviceNames.Length; i = i + 1)
		{
			string reserved = ReservedDeviceNames[i];

			if (string.Equals(candidate, reserved, StringComparison.OrdinalIgnoreCase))
			{
				candidate = candidate + "_file";
				break;
			}
		}

		// Last fallback: ensure not empty
		if (string.IsNullOrEmpty(candidate))
		{
			return string.Empty;
		}

		return candidate;
	}
}
