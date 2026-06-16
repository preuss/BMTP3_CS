using MediaDevices;
using System.Runtime.Versioning;

namespace BMTP3.Core4.Devices;

[SupportedOSPlatform("windows7.0")]
internal sealed class MediaDirectory : IMediaDirectory
{
	private readonly MediaDirectoryInfo _directory;

	private IReadOnlyList<IMediaDirectory>? _directories;
	private IReadOnlyList<IMediaFile>? _files;

	public MediaDirectory(MediaDirectoryInfo directory)
	{
		_directory = directory ?? throw new ArgumentNullException(nameof(directory));
	}

	public string FullName => _directory.FullName;
	public string Name => _directory.Name;
	public DateTime? CreationTime => _directory.CreationTime;
	public DateTime? LastWriteTime => _directory.LastWriteTime;
	public DateTime? DateAuthored => _directory.DateAuthored;
	public MediaFileAttribute Attributes => (MediaFileAttribute)_directory.Attributes;
	public string Id => _directory.Id;
	public string PersistentUniqueId => _directory.PersistentUniqueId;
	public IReadOnlyList<IMediaDirectory> Directories =>
		_directories ??= _directory
			.EnumerateDirectories()
			.Select(d => (IMediaDirectory)new MediaDirectory(d))
			.ToArray();

	public IReadOnlyList<IMediaFile> Files =>
		_files ??= _directory
			.EnumerateFiles()
			.Select(f => (IMediaFile)new MediaFile(f))
			.ToArray();

	public IEnumerable<IMediaFile> EnumerateFiles() =>
		_directory.EnumerateFiles().Select(f => (IMediaFile)new MediaFile(f));

	public IEnumerable<IMediaDirectory> EnumerateDirectories() =>
		_directory.EnumerateDirectories().Select(d => (IMediaDirectory)new MediaDirectory(d));
}