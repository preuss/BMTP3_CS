using System;
using System.IO;
using System.Linq;

namespace BMTP3.Core2.BackupNew.Utilities;

/// <summary>
/// Helpers for normalizing file/device paths into stable URIs used in metadata.
/// </summary>
public static class PathNormalizer
{
	/// <summary>
	/// Normalize a local filesystem path into a file:// URI string.
	/// Examples: "file:///C:/folder/file.jpg" or "file://server/share/file.jpg".
	/// </summary>
	public static string NormalizeFileUrl(string path)
	{
		if(string.IsNullOrEmpty(path))
		{
			return string.Empty;
		}

		try
		{
			string full = Path.GetFullPath(path);
			return new Uri(full).AbsoluteUri; // e.g. "file:///C:/folder/file.jpg" or "file://server/share/file.jpg"
		} catch
		{
			// Fallback: attempt to create a file:// URI from the original path
			Uri uri = new Uri(path, UriKind.RelativeOrAbsolute);
			return uri.IsAbsoluteUri ? uri.AbsoluteUri : new Uri(Path.GetFullPath(path)).AbsoluteUri;
		}
	}

	/// <summary>
	/// Normalize a device (MTP) path into an mtp:// URI string using the provided deviceId as authority.
	/// Example: "mtp://deviceId/DCIM/100APPLE/IMG_0001.JPG".
	/// </summary>
	public static string NormalizeDeviceFileUrl(string path, string deviceId)
	{
		if(string.IsNullOrEmpty(deviceId))
		{
			throw new ArgumentNullException(nameof(deviceId));
		}

		if(string.IsNullOrEmpty(path))
		{
			return $"mtp://{Uri.EscapeDataString(deviceId)}/";
		}

		string mtpPath = path.TrimStart('\\', '/').Replace('\\', '/');
		IEnumerable<string> segments = mtpPath
			.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
			.Select(s => Uri.EscapeDataString(s));

		string escapedDevice = Uri.EscapeDataString(deviceId);
		return segments.Any()
			? $"mtp://{escapedDevice}/{string.Join('/', segments)}"
			: $"mtp://{escapedDevice}/";
	}
}
