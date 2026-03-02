using System;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace BMTP3.Core2.BackupNew.exifreader.Extensions;

/// <summary>
/// Lightweight safe-access extension methods for metadata directories.
/// These helpers avoid throwing when a tag is missing or malformed.
/// </summary>
public static class MetadataDirectoryExtensions
{
	/// <summary>
	/// Return the string value for <paramref name="tagType"/> or <c>null</c> when not available or on error.
	/// </summary>
	public static string? SafeGetString(this MetadataExtractor.Directory? dir, int tagType)
	{
		if (dir is null)
			return null;

		try
		{
			return dir.GetString(tagType);
		}
		catch (Exception)
		{
			// Intentionally swallow exceptions coming from the metadata library and
			// treat them as "no value". This mirrors previous behaviour but keeps
			// the surface area small so callers don't need to wrap every call.
			return null;
		}
	}

	/// <summary>
	/// Try read a DateTime value from an EXIF directory in a safe manner.
	/// </summary>
	public static bool SafeTryGetDateTime(this ExifDirectoryBase? dir, int tagType, out DateTime dt)
	{
		dt = default;
		if (dir is null)
			return false;

		try
		{
			return dir.TryGetDateTime(tagType, out dt);
		}
		catch (Exception)
		{
			// Treat any error as missing value.
			return false;
		}
	}
}
