namespace BMTP3.Core4.Devices;
internal interface IMediaDrive
{
	long AvailableFreeSpace { get; }
	string DriveFormat { get; }
	string DriveType { get; }
	bool IsReady { get; }
	string Name { get; }
	IMediaDirectory? RootDirectory { get; }
	long TotalFreeSpace { get; }
	long TotalSize { get; }
	string VolumeLabel { get; }
}
