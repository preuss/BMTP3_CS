namespace BMTP3.Core4.Traversal;

public interface IMediaDeviceSession : IDisposable
{
	string DeviceName { get; }
}