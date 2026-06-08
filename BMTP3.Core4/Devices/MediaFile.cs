using MediaDevices;
using System.Runtime.Versioning;

namespace BMTP3.Core4.Devices;

[SupportedOSPlatform("windows7.0")]
internal sealed class MediaFile : IMediaFile
{
	private readonly MediaFileInfo _file;

	public MediaFile(MediaFileInfo file)
	{
		_file = file ?? throw new ArgumentNullException(nameof(file));
	}

	public string FullName => _file.FullName;
	public string Name => _file.Name;
	public DateTime? CreationTime => _file.CreationTime;
	public DateTime? LastWriteTime => _file.LastWriteTime;
	public DateTime? DateAuthored => _file.DateAuthored;
	public MediaFileAttribute Attributes => (MediaFileAttribute)_file.Attributes;
	public string Id => _file.Id;
	public string PersistentUniqueId => _file.PersistentUniqueId;
	public ulong Length => _file.Length;

	public Stream OpenRead() => _file.OpenRead();
}