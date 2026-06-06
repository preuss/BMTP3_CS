namespace BMTP3.Core4.Traversal;

public interface IMtpDeviceSession : IDisposable
{
	string DeviceName { get; }
}