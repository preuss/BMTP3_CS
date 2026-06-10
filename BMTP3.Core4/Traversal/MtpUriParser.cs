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

		string remainder = uri.Substring(Scheme.Length);

		int firstSlash = remainder.IndexOf('/');

		string deviceName;
		string afterDevice;

		if(firstSlash == -1)
		{
			deviceName = remainder;
			afterDevice = string.Empty;
		} else
		{
			deviceName = remainder.Substring(0, firstSlash);
			afterDevice = remainder.Substring(firstSlash + 1);
		}

		if(string.IsNullOrWhiteSpace(deviceName))
		{
			throw new ArgumentException($"Device name is empty in URI: '{uri}'", nameof(uri));
		}

		int secondSlash = afterDevice.IndexOf('/');
		string driveName;
		string directoryPath;

		if(string.IsNullOrEmpty(afterDevice))
		{
			driveName = string.Empty;
			directoryPath = string.Empty;
		}
		else if(secondSlash == -1)
		{
			driveName = afterDevice;
			directoryPath = string.Empty;
		}
		else
		{
			driveName = afterDevice.Substring(0, secondSlash);
			directoryPath = afterDevice.Substring(secondSlash + 1);

			if(directoryPath.Length > 0 && directoryPath.EndsWith("/"))
			{
				directoryPath = directoryPath.TrimEnd('/');
			}
		}

		return new MtpUriParseResult(deviceName, driveName, directoryPath);
	}
}
