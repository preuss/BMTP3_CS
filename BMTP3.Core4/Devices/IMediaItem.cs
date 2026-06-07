namespace BMTP3.Core4.Devices;
internal interface IMediaItem
{
	string FullName { get; }
	string Name { get; }
	DateTime? CreationTime { get; }
	DateTime? LastWriteTime { get; }
	DateTime? DateAuthored { get; }
	MediaFileAttribute Attributes { get; }
	string Id { get; }
	string PersistentUniqueId { get; }
}
