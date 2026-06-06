namespace BMTP3.Core4.Traversal;

public static class MtpUriParser
{
	private const string Scheme = "mtp://";

	public static MtpUriParseResult Parse(string uri)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(uri);

		if(!uri.StartsWith(Scheme, StringComparison.Ordinal))
		{
			throw new ArgumentException($"URI must start with '{Scheme}'. Got: '{uri}'", nameof(uri));
		}

		// Remove scheme
		string remainder = uri.Substring(Scheme.Length);

		int slashIndex = remainder.IndexOf('/');

		string deviceName;
		string devicePath;

		if(slashIndex == -1)
		{
			// No slash, so the entire remainder is the device name
			deviceName = remainder;
			devicePath = string.Empty;
		} else
		{
			// Split clearly and explicitly
			deviceName = remainder.Substring(0, slashIndex);
			devicePath = remainder.Substring(slashIndex + 1);

			// Trim trailing slash if necessary
			if(devicePath.Length > 0 && devicePath.EndsWith("/"))
			{
				devicePath = devicePath.TrimEnd('/');
			}
		}

		if(string.IsNullOrWhiteSpace(deviceName))
		{
			throw new ArgumentException($"Device name is empty in URI: '{uri}'", nameof(uri));
		}

		return new MtpUriParseResult(deviceName, devicePath);
	}
}
