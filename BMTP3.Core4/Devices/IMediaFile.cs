namespace BMTP3.Core4.Devices;

internal interface IMediaFile : IMediaItem
{
	ulong Length { get; }
}