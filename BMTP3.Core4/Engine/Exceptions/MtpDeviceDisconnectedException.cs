namespace BMTP3.Core4.Engine.Exceptions;

public sealed class MtpDeviceDisconnectedException : Exception
{
	public int HResultValue { get; }

	public MtpDeviceDisconnectedException(string message, int hResult, Exception innerException)
		: base(message, innerException)
	{
		HResultValue = hResult;
	}
}
