namespace BMTP3.Core4.Devices;

internal sealed class MediaDeviceException : Exception
{
	public MediaDeviceException(string message) : base(message)
	{
	}

	public MediaDeviceException(string message, Exception innerException) : base(message, innerException)
	{
	}
}
