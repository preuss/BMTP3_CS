namespace BMTP3.Core4.Devices;

internal interface IMediaDirectory : IMediaItem
{
	IReadOnlyList<IMediaDirectory> Directories { get; }
	IReadOnlyList<IMediaFile> Files { get; }
}
