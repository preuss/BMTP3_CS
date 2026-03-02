using System.Text;

namespace BMTP3.Core2.BackupNew.Utilities;

/// <summary>
/// Helpers for normalizing file/device paths into stable URIs used in metadata.
/// </summary>
public static class PathNormalizer
{
	/// <summary>
	/// Normalize a local filesystem path into a file:// URI string.
	/// Examples: "file:///C:/folder/file.jpg" or "file://server/share/file.jpg".
	/// Returns empty string on null/empty input.
	/// </summary>
	public static string NormalizeFileUri(string path)
	{
		if(string.IsNullOrWhiteSpace(path))
		{
			return string.Empty;
		}

		try
		{
			// Canonicalize and let Uri handle UNC vs drive letters and escaping
			string full = Path.GetFullPath(path);
			Uri uri = new(full);

			// Defensive: only accept absolute file:// URIs (avoid accidentally persisting non-file schemes in metadata).
			if(!uri.IsAbsoluteUri || !string.Equals(uri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
			{
				return string.Empty;
			}
			return uri.AbsoluteUri; // e.g. "file:///C:/folder/file.jpg" or "file://server/share/file.jpg"
		} catch(ArgumentException)
		{
			return string.Empty;
		} catch(NotSupportedException)
		{
			return string.Empty;
		} catch(PathTooLongException)
		{
			return string.Empty;
		} catch(IOException)
		{
			return string.Empty;
		} catch(System.Security.SecurityException)
		{
			return string.Empty;
		} catch(UriFormatException)
		{
			return string.Empty;
		}
	}

	/// <summary>
	/// Normalize a device (MTP) path into an mtp:// URI string using the provided deviceId as authority.
	/// Example: "mtp://deviceId/DCIM/100APPLE/IMG_0001.JPG".
	/// </summary>
	public static string NormalizeMtpUri(string path, string deviceId)
	{
		if(string.IsNullOrEmpty(deviceId))
		{
			throw new ArgumentNullException(nameof(deviceId));
		}

		string escapedDevice = Uri.EscapeDataString(deviceId);

		if(string.IsNullOrEmpty(path))
		{
			return $"mtp://{escapedDevice}/";
		}

		// Normalize separators and trim leading slashes
		string mtpPath = path.Replace('\\', '/').TrimStart('/');

		// Split into segments, resolve dot segments ('.' and '..') and normalize unicode
		string[] rawSegments = mtpPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> stack = new List<string>(rawSegments.Length);
		foreach(string seg in rawSegments)
		{
			if(seg == ".")
			{
				continue;
			}
			if(seg == "..")
			{
				if(stack.Count > 0)
				{
					stack.RemoveAt(stack.Count - 1);
				}
				continue;
			}

			// Normalize unicode to NFC for stable representation
			string normalized = seg.Normalize(NormalizationForm.FormC);
			stack.Add(normalized);
		}

		IEnumerable<string> escapedSegments = stack.Select(s => Uri.EscapeDataString(s));
		return escapedSegments.Any()
			? $"mtp://{escapedDevice}/{string.Join('/', escapedSegments)}"
			: $"mtp://{escapedDevice}/";
	}
}
